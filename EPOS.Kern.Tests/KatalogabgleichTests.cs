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
            Assert.Equal(new[] { "Tab_Prozesswaerme_STAMM", "Tab_Prozesstyp_STAMM", "Tab_Konditionierungsvorlage_STAMM", "Tab_Wechselrichter_STAMM" },
                         p.Tabellen.Select(t => t.Tabelle).ToArray());
            Assert.Equal(16 + 14 + 1, p.Satzzahl);
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
            Assert.Equal(PufferOptionenSchema.SCHRITT + 1, KatalogfassungSchema.SCHRITT);
            Assert.Equal(172, KatalogfassungSchema.SCHRITT);   // 170 Hilfsenergie, 171 Pufferoptionen
            Assert.True(SchemaStand.Zielversion >= KatalogfassungSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Katalog, Paketanhebung.Stufen.Single(x => x.Nr == KatalogfassungSchema.SCHRITT).Wirkung);
            Assert.Equal(8, Katalogfassung.Stufe1.Count);
            Assert.Equal(Katalogfassung.Stufe1.Count, Katalogfassung.Stufe1.Select(t => t.Kuerzel).Distinct().Count());

            // Stufe 2: der nächste Schritt, dieselbe Paketstufe, sechzehn Kopftabellen, Kürzel eindeutig.
            Assert.Equal(KatalogfassungSchema.SCHRITT + 1, KatalogfassungStufe2Schema.SCHRITT);
            Assert.Equal(173, KatalogfassungStufe2Schema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= KatalogfassungStufe2Schema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Katalog,
                         Paketanhebung.Stufen.Single(x => x.Nr == KatalogfassungStufe2Schema.SCHRITT).Wirkung);
            Assert.Equal(16, Katalogfassung.Stufe2.Count);
            Assert.All(Katalogfassung.Stufe2, t => Assert.Equal(2, t.Stufe));
            Assert.Equal(Katalogfassung.Alle.Count, Katalogfassung.Alle.Select(t => t.Kuerzel).Distinct().Count());
            Assert.Equal(Katalogfassung.Alle.Count, Katalogfassung.Alle.Select(t => t.Tabelle).Distinct().Count());
            Assert.All(Katalogfassung.Alle, t => Assert.False(string.IsNullOrEmpty(t.Anzeigeschluessel)));
        }

        /// <summary>Der Katalog in Worten kommt aus dem Register, für jede Tabelle in beiden Sprachen.</summary>
        [Fact]
        public void Jeder_Katalog_des_Registers_hat_einen_Namen_in_beiden_Sprachen()
        {
            CultureInfo vorher = CultureInfo.CurrentUICulture;
            try
            {
                foreach (string kultur in new[] { "de-DE", "en-US" })
                {
                    CultureInfo.CurrentUICulture = new CultureInfo(kultur);
                    foreach (Katalogtabelle t in Katalogfassung.Alle)
                    {
                        string name = Katalogfassung.Anzeigename(t.Tabelle);
                        Assert.False(string.IsNullOrWhiteSpace(name));
                        Assert.NotEqual(t.Tabelle, name);
                    }
                }
            }
            finally { CultureInfo.CurrentUICulture = vorher; }
            Assert.Equal("Tab_Gibt_Es_Nicht", Katalogfassung.Anzeigename("Tab_Gibt_Es_Nicht"));
        }

        /// <summary>
        /// Eine REIHE geht in ihrer Folge ein — vertauschte Werte ändern die Prüfsumme; die Kennlinie
        /// (keine Reihe) bleibt reihenfolgefest. Die Enkel einer Kindzeile gehen sortiert ein.
        /// </summary>
        [Fact]
        public void Reihen_gehen_geordnet_und_Enkel_sortiert_ein()
        {
            Katalogtabelle wb = Katalogfassung.Tabelle("Tab_Waermebedarf_STAMM");
            Assert.True(wb.Kinder.Single().Reihe);
            var kopf = new Dictionary<string, object> { ["Bezeichner"] = "Ganglinie" };
            IReadOnlyDictionary<string, object> a = new Dictionary<string, object> { ["Wert"] = 1.5 };
            IReadOnlyDictionary<string, object> b = new Dictionary<string, object> { ["Wert"] = 2.5 };
            string ab = Katalogfassung.Pruefsumme(wb, kopf, new Dictionary<string, IReadOnlyList<IReadOnlyDictionary<string, object>>>
                { ["Tab_WaermebedarfDaten_STAMM"] = new[] { a, b } });
            string ba = Katalogfassung.Pruefsumme(wb, kopf, new Dictionary<string, IReadOnlyList<IReadOnlyDictionary<string, object>>>
                { ["Tab_WaermebedarfDaten_STAMM"] = new[] { b, a } });
            Assert.NotEqual(ab, ba);

            Katalogtabelle kv = Katalogfassung.Tabelle("Tab_Konditionierungsvorlage_STAMM");
            var vorlage = new Dictionary<string, object> { ["Groesse"] = "HEIZSOLL", ["Bezeichner"] = "Muster" };
            Dictionary<string, object> P(long rang, double wert) =>
                new Dictionary<string, object> { ["Rang"] = rang, ["Art"] = "FEIERTAG", ["Bezeichner"] = "F" + rang, ["Wert"] = wert };
            IReadOnlyDictionary<string, object> Kalender(params Dictionary<string, object>[] perioden) =>
                new Dictionary<string, object> { ["Groesse"] = "HEIZSOLL", ["Wert"] = 20.0, ["Tab_Konditionierungsperiode"] = perioden.ToList() };
            string Summe(IReadOnlyDictionary<string, object> kal) => Katalogfassung.Pruefsumme(kv, vorlage,
                new Dictionary<string, IReadOnlyList<IReadOnlyDictionary<string, object>>> { ["Tab_Konditionierungskalender"] = new[] { kal } });
            Assert.Equal(Summe(Kalender(P(101, 16), P(102, 16))), Summe(Kalender(P(102, 16), P(101, 16))));
            Assert.NotEqual(Summe(Kalender(P(101, 16), P(102, 16))), Summe(Kalender(P(101, 16), P(102, 17))));
            Assert.NotEqual(Summe(Kalender(P(101, 16))), Summe(Kalender(P(101, 16), P(102, 16))));

            Assert.Equal("KV:HEIZSOLL_MUSTER", Katalogfassung.Schluesselstamm(kv, Katalogfassung.Name(kv, vorlage)));
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
        public void Wache_die_Tabellenliste_des_Registers_haelt_das_Schema()
        {
            if (!_db.Vorhanden) return;

            foreach (Katalogtabelle t in Katalogfassung.Alle)
            {
                Assert.True(DataRepository.TabelleVorhanden(t.Tabelle), t.Tabelle + " fehlt.");
                List<string> schema = DataRepository.SpaltenVonTabelle(t.Tabelle);
                foreach (string s in t.Fachspalten)
                    Assert.True(schema.Contains(s), t.Tabelle + "." + s + " steht nicht im Schema.");
                foreach (string s in schema)
                    Assert.True(t.Fachspalten.Contains(s) || Katalogfassung.Metaspalten.Contains(s),
                                t.Tabelle + "." + s + " ist weder Fach- noch Metaspalte des Registers.");
                Assert.Contains("ReadOnly", schema);
                foreach (string s in t.Namensspalten) Assert.Contains(s, t.Fachspalten);
                foreach (Katalogverweis v in t.Verweise) Verweis(t.Tabelle, t.Fachspalten, v);

                foreach (Katalogkind k in Katalogfassung.KinderUndEnkel(t))
                {
                    Assert.True(DataRepository.TabelleVorhanden(k.Tabelle), k.Tabelle + " fehlt.");
                    List<string> kind = DataRepository.SpaltenVonTabelle(k.Tabelle);
                    Assert.Contains(k.Fremdschluessel, kind);
                    if (k.Projektspalte != null) Assert.Contains(k.Projektspalte, kind);
                    foreach (string s in k.Fachspalten)
                        Assert.True(kind.Contains(s), k.Tabelle + "." + s + " steht nicht im Schema.");
                    foreach (string s in kind)
                        Assert.True(k.Fachspalten.Contains(s) || s == "ID" || s == "ReadOnly" || s == k.Fremdschluessel ||
                                    s == k.Projektspalte || k.Nebenspalten.Contains(s),
                                    k.Tabelle + "." + s + " ist keine Fachspalte der Kindtabelle.");
                    foreach (Katalogverweis v in k.Verweise) Verweis(k.Tabelle, k.Fachspalten, v);
                    if (k.GesperrtEinfuegen) Assert.Contains("ReadOnly", kind);
                }
            }
        }

        private static void Verweis(string tabelle, IReadOnlyList<string> fachspalten, Katalogverweis v)
        {
            Assert.True(fachspalten.Contains(v.Spalte), tabelle + "." + v.Spalte + ": Verweis ohne Fachspalte.");
            Assert.True(DataRepository.SpalteVorhanden(v.Zieltabelle, v.Zielspalte),
                        tabelle + "." + v.Spalte + ": Ziel " + v.Zieltabelle + "." + v.Zielspalte + " fehlt.");
            // Ein Ziel im Register steht VOR dem Verweiser - die Saat und der Abgleich belegen es zuerst.
            Katalogtabelle ziel = Katalogfassung.Tabelle(v.Zieltabelle);
            if (ziel != null)
            {
                int i = Katalogfassung.Alle.ToList().FindIndex(x => x.Tabelle == ziel.Tabelle);
                int j = Katalogfassung.Alle.ToList().FindIndex(x => x.Tabelle == tabelle ||
                                                                    Katalogfassung.KinderUndEnkel(x).Any(k => k.Tabelle == tabelle));
                Assert.True(i < j, v.Zieltabelle + " muss vor " + tabelle + " im Register stehen.");
            }
        }

        /// <summary>
        /// <b>Die Wache der Vollständigkeit:</b> Jede <c>_STAMM</c>-Tabelle der Testdatenbank steht im
        /// Register (als Kopf, Kind oder Enkel) oder benannt in <see cref="Katalogfassung.Ausgenommen"/> —
        /// nie beides, und jede Ausnahme nennt einen Grund. Ein neuer Katalog macht sie rot.
        /// </summary>
        [Fact]
        public void Wache_jede_Stammtabelle_ist_im_Register_oder_benannt_ausgenommen()
        {
            if (!_db.Vorhanden) return;

            var erfasst = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (Katalogtabelle t in Katalogfassung.Alle)
            {
                erfasst.Add(t.Tabelle);
                foreach (Katalogkind k in Katalogfassung.KinderUndEnkel(t)) erfasst.Add(k.Tabelle);
            }
            DataTable dt = DataRepository.GetDataTable(
                "SELECT name FROM sqlite_master WHERE type = 'table' AND upper(name) LIKE '%\\_STAMM' ESCAPE '\\' ORDER BY name");
            var stamm = dt.Rows.Cast<DataRow>().Select(r => Convert.ToString(r[0])).ToList();
            // KU3-1: Tab_Kaeltemaschine_STAMM und Tab_Kenndaten_Kaeltemaschine_STAMM dazu - 46.
            Assert.Equal(46, stamm.Count);
            foreach (string s in stamm)
                Assert.True(erfasst.Contains(s) ^ Katalogfassung.Ausgenommen.ContainsKey(s),
                            s + " steht weder im Register noch in den Ausnahmen (oder in beiden).");
            Assert.All(Katalogfassung.Ausgenommen, kv => Assert.True(kv.Value.Length > 40, kv.Key + ": Grund fehlt."));
            Assert.Equal(12, Katalogfassung.Ausgenommen.Count);
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

            foreach (Katalogtabelle t in Katalogfassung.Alle)
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
            Assert.Equal(11 + 15, e.Unveraendert);                          // acht + acht Prozesswärme, 14 Vorlagen und ein Wechselrichter
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
        /// <summary>
        /// UB-E3-b (Umsetzungskonzept 4.3): Die neun Gerätespalten der Übergabegrenze stehen in Stufe 1 — leer ändern sie
        /// keine Prüfsumme (die ausgelieferten Sätze bleiben gleich), ein gepflegter Wert ändert sie.
        /// </summary>
        [Fact]
        public void Leere_Geraetegrenzen_aendern_keine_Pruefsumme_ein_gepflegter_Wert_schon()
        {
            Katalogtabelle wp = Katalogfassung.Tabelle("Tab_WP_STAMM");
            Katalogtabelle bhkw = Katalogfassung.Tabelle("Tab_BHKW_STAMM");
            foreach ((string spalte, string _) in UebergabegrenzeSchema.WP_SPALTEN)
                Assert.Contains(spalte, wp.Fachspalten);
            Assert.Contains(UebergabegrenzeSchema.SPALTE_RUECKLAUF_MAX, bhkw.Fachspalten);

            var ohne = new Dictionary<string, object> { ["Bezeichner"] = "WP A", ["Nennleistung"] = 10L };
            var leer = new Dictionary<string, object>(ohne);
            foreach ((string spalte, string _) in UebergabegrenzeSchema.WP_SPALTEN) leer[spalte] = null;
            Assert.Equal(Katalogfassung.Pruefsumme(wp, ohne), Katalogfassung.Pruefsumme(wp, leer));
            Assert.NotEqual(Katalogfassung.Pruefsumme(wp, ohne),
                            Katalogfassung.Pruefsumme(wp, new Dictionary<string, object>(leer) { ["Spreizung_Min_K"] = 4.0 }));
            Assert.NotEqual(Katalogfassung.Pruefsumme(wp, ohne),
                            Katalogfassung.Pruefsumme(wp, new Dictionary<string, object>(leer) { ["Kaeltemittel"] = "R744" }));

            var b = new Dictionary<string, object> { ["Bezeichner"] = "BHKW A" };
            Assert.Equal(Katalogfassung.Pruefsumme(bhkw, b),
                         Katalogfassung.Pruefsumme(bhkw, new Dictionary<string, object>(b) { ["Ruecklauf_Max"] = null }));
            Assert.NotEqual(Katalogfassung.Pruefsumme(bhkw, b),
                            Katalogfassung.Pruefsumme(bhkw, new Dictionary<string, object>(b) { ["Ruecklauf_Max"] = 70.0 }));
        }

        /// <summary>
        /// UB-E3-b: Ein gepflegter Gerätewert hebt die Katalogfassung über den gewohnten Weg — das festgeschriebene
        /// Paket der nächsten Fassung trägt den Wert, die Prüfsumme des Satzes wechselt, alle übrigen Sätze bleiben.
        /// Das ist zugleich der Weg der Auslieferungsvorlage (<c>Katalogpaket.json</c> aus <see cref="Katalogpaket.Festschreiben"/>).
        /// </summary>
        [Fact]
        public void Ein_gepflegter_Geraetewert_hebt_die_Katalogfassung()
        {
            if (!_db.Vorhanden) return;

            Katalogpaket vorher = Katalogpaket.Festschreiben(7);
            Katalogpakettabelle tv = vorher.Tabellen.Single(t => t.Tabelle == "Tab_WP_STAMM");
            Katalogpaketsatz sv = tv.Saetze.First();
            Assert.False(sv.Werte.TryGetValue("Spreizung_Min_K", out object leerWert) && leerWert != null);

            DataRepository.ExecuteNonQuery("UPDATE Tab_WP_STAMM SET Spreizung_Min_K = 4, Kaeltemittel = 'R290' WHERE Katalog_Schluessel = ?",
                                           new DbParam("@s", sv.Schluessel));
            Katalogpaket nachher = Katalogpaket.Festschreiben(8);
            Assert.Equal(8, Katalogabgleich.FassungDerDatenbank());
            Katalogpaketsatz sn = nachher.Tabellen.Single(t => t.Tabelle == "Tab_WP_STAMM").Saetze.Single(s => s.Schluessel == sv.Schluessel);
            Assert.NotEqual(sv.Pruefsumme, sn.Pruefsumme);
            Assert.Equal(4.0, Convert.ToDouble(sn.Werte["Spreizung_Min_K"], CultureInfo.InvariantCulture));
            Assert.Equal("R290", Convert.ToString(sn.Werte["Kaeltemittel"], CultureInfo.InvariantCulture));

            int gleich = tv.Saetze.Count(s => nachher.Tabellen.Single(t => t.Tabelle == "Tab_WP_STAMM").Saetze
                                                     .Any(n => n.Schluessel == s.Schluessel && n.Pruefsumme == s.Pruefsumme));
            Assert.Equal(tv.Saetze.Count - 1, gleich);
        }

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
        //  Teil 4 - Stufe 2: Reihen, Enkel, Verweise
        // =============================================================================

        /// <summary>Die Formatversion 1 liest sich weiter; eine unbekannte spätere nicht.</summary>
        [Fact]
        public void Formatversion_1_liest_sich_und_eine_spaetere_nicht()
        {
            string text = File.ReadAllText(Probe, new UTF8Encoding(false));
            Assert.Contains("\"Formatversion\": " + Katalogpaket.FORMATVERSION, text);
            Katalogpaket alt = Katalogpaket.AusBytes(new UTF8Encoding(false).GetBytes(
                text.Replace("\"Formatversion\": 2", "\"Formatversion\": 1")));
            Assert.Equal(31, alt.Satzzahl);
            Assert.Throws<InvalidDataException>(() => Katalogpaket.AusBytes(new UTF8Encoding(false).GetBytes(
                text.Replace("\"Formatversion\": 2", "\"Formatversion\": 3"))));
        }

        /// <summary>
        /// Das Paket der ganzen Testkopie liest sich verlustfrei zurück — Reihen als Werteliste, die
        /// Perioden der Konditionierungskalender als Enkel —, und eine Reihe steht in ihrer Folge.
        /// </summary>
        [Fact]
        public void Paket_der_Stufe_2_liest_sich_byte_gleich_zurueck()
        {
            if (!_db.Vorhanden) return;

            Katalogpaket p = Katalogpaket.AusDatenbank(3);
            byte[] bytes = p.Bytes();
            Katalogpaket zurueck = Katalogpaket.AusBytes(bytes);
            Assert.Equal(bytes, zurueck.Bytes());

            Katalogpaketsatz wb = zurueck.Tabellen.Single(t => t.Tabelle == "Tab_Waermebedarf_STAMM").Saetze.First();
            List<Dictionary<string, object>> reihe = wb.Kinder["Tab_WaermebedarfDaten_STAMM"];
            Assert.Equal(8760, reihe.Count);
            long id = Zahl("SELECT ID FROM Tab_Waermebedarf_STAMM WHERE Katalog_Schluessel = '" + wb.Schluessel + "'");
            DataTable dt = DataRepository.GetDataTable("SELECT Wert FROM Tab_WaermebedarfDaten_STAMM WHERE ID_Ganglinie = ? ORDER BY ID",
                                                       new DbParam("@id", id));
            for (int i = 0; i < 8760; i += 997)
                Assert.Equal(Convert.ToDouble(dt.Rows[i][0], CultureInfo.InvariantCulture),
                             Convert.ToDouble(reihe[i]["Wert"], CultureInfo.InvariantCulture));

            Katalogpaketsatz kv = zurueck.Tabellen.Single(t => t.Tabelle == "Tab_Konditionierungsvorlage_STAMM")
                                         .Saetze.Single(s => s.Schluessel == "KV:HEIZSOLL_BUERO");
            Assert.Equal("HEIZSOLL / Büro", kv.Bezeichner);
            Assert.Equal(9, Katalogfassung.Enkelzeilen(kv.Kinder["Tab_Konditionierungskalender"].Single(),
                                                       "Tab_Konditionierungsperiode").Count);

            // Der Verweis des Brennstoffs steht mit dem Namen seiner Kategorie, nicht mit ihrer ID.
            Katalogtabelle brs = Katalogfassung.Tabelle("Tab_Brennstoff_Stamm");
            DataTable b = DataRepository.GetDataTable("SELECT ID, " + string.Join(", ", brs.Fachspalten.Select(s => "\"" + s + "\"")) +
                                                      " FROM Tab_Brennstoff_Stamm WHERE Bezeichner = 'Stadtgas'");
            Dictionary<string, object> werte = Katalogfassung.Fachwerte(brs, brs.Fachspalten, b.Rows[0], Katalogfassung.LeseOhneVorgang);
            Assert.Equal("Gas", werte["ID_Kategorie"]);
        }

        /// <summary>
        /// <b>Der Abgleich der Stufe 2:</b> Eine Vorlage mit geänderter Periode wird aktualisiert (Kalender
        /// samt Perioden neu), eine fehlende Wärmebedarfsganglinie samt ihrer 8 760 Werte in ihrer Folge
        /// eingefügt; ein zweiter, erzwungener Lauf findet nichts mehr zu tun.
        /// </summary>
        [Fact]
        public void Abgleich_der_Stufe_2_mit_Perioden_und_Reihe()
        {
            if (!_db.Vorhanden) return;

            Katalogpaket paket = Katalogpaket.AusBytes(Katalogpaket.AusDatenbank(2).Bytes());
            Katalogtabelle kvT = Katalogfassung.Tabelle("Tab_Konditionierungsvorlage_STAMM");
            Katalogpaketsatz kv = paket.Tabellen.Single(t => t.Tabelle == kvT.Tabelle).Saetze.Single(s => s.Schluessel == "KV:HEIZSOLL_BUERO");
            var periode = (Dictionary<string, object>)Katalogfassung.Enkelzeilen(
                kv.Kinder["Tab_Konditionierungskalender"].Single(), "Tab_Konditionierungsperiode")[0];
            periode["Bezeichner"] = "Neuer Name";
            kv.Pruefsumme = Neusumme(kvT, kv);

            Katalogpaketsatz wb = paket.Tabellen.Single(t => t.Tabelle == "Tab_Waermebedarf_STAMM").Saetze.First();
            long wbId = Zahl("SELECT ID FROM Tab_Waermebedarf_STAMM WHERE Katalog_Schluessel = '" + wb.Schluessel + "'");
            string reiheVorher = Tabellenbild2("SELECT Wert FROM Tab_WaermebedarfDaten_STAMM WHERE ID_Ganglinie = " + wbId + " ORDER BY ID");
            DataRepository.ExecuteNonQuery("DELETE FROM Tab_WaermebedarfDaten_STAMM WHERE ID_Ganglinie = ?", new DbParam("@id", wbId));
            DataRepository.ExecuteNonQuery("DELETE FROM Tab_Waermebedarf_STAMM WHERE ID = ?", new DbParam("@id", wbId));
            string projektVorher = Projektbild();

            KatalogabgleichErgebnis e = Katalogabgleich.Ausfuehren(paket, nurPruefen: false);
            Assert.True(e.Ausgefuehrt, e.Meldung);
            Assert.Equal((1, 1, 0, 0), (e.Neu, e.Aktualisiert, e.Behalten, e.Ausgelaufen));

            long kvId = Zahl("SELECT ID FROM Tab_Konditionierungsvorlage_STAMM WHERE Katalog_Schluessel = 'KV:HEIZSOLL_BUERO'");
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Konditionierungskalender WHERE ID_Vorlage = " + kvId));
            Assert.Equal(9L, Zahl("SELECT COUNT(*) FROM Tab_Konditionierungsperiode p JOIN Tab_Konditionierungskalender k " +
                                  "ON k.ID = p.ID_Kalender WHERE k.ID_Vorlage = " + kvId));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Konditionierungsperiode p JOIN Tab_Konditionierungskalender k " +
                                  "ON k.ID = p.ID_Kalender WHERE k.ID_Vorlage = " + kvId + " AND p.Bezeichner = 'Neuer Name'"));
            Assert.Equal(kv.Pruefsumme, Text("SELECT Katalog_Pruefsumme FROM Tab_Konditionierungsvorlage_STAMM WHERE ID = " + kvId));

            long neu = Zahl("SELECT ID FROM Tab_Waermebedarf_STAMM WHERE Katalog_Schluessel = '" + wb.Schluessel + "' AND \"ReadOnly\" = 1");
            Assert.Equal(reiheVorher, Tabellenbild2("SELECT Wert FROM Tab_WaermebedarfDaten_STAMM WHERE ID_Ganglinie = " + neu + " ORDER BY ID"));
            Assert.Equal(projektVorher, Projektbild());

            KatalogabgleichErgebnis zweiter = Katalogabgleich.Ausfuehren(paket, nurPruefen: true, erzwingen: true);
            Assert.False(zweiter.EtwasZuTun);
            Assert.Equal(0, zweiter.Behalten);
        }

        /// <summary>
        /// Ein Verweis überlebt den Weg über das Paket: Die Schicht eines ausgelieferten Bauteilaufbaus
        /// zeigt nach dem Einfügen auf denselben Baustoff — über dessen Schlüssel, nicht über die ID.
        /// </summary>
        [Fact]
        public void Verweis_der_Bauteilschicht_geht_ueber_den_Schluessel_des_Baustoffs()
        {
            if (!_db.Vorhanden) return;

            long baustoff = Zahl("SELECT ID FROM Tab_Baustoff_STAMM WHERE Bezeichner = 'Kalkzementputz'");
            string schluessel = Text("SELECT Katalog_Schluessel FROM Tab_Baustoff_STAMM WHERE ID = " + baustoff);
            Assert.StartsWith("BST:", schluessel);
            DataRepository.ExecuteNonQuery("INSERT INTO Tab_Bauteilaufbau_STAMM (Bezeichner, Bauteilart, Herkunft, \"ReadOnly\") " +
                                           "VALUES ('Probewand', 'AUSSENWAND', 'VORGABE', 1)");
            long aufbau = Zahl("SELECT ID FROM Tab_Bauteilaufbau_STAMM WHERE Bezeichner = 'Probewand'");
            DataRepository.ExecuteNonQuery("INSERT INTO Tab_Bauteilschicht_STAMM (ID_Aufbau, Reihenfolge, ID_Baustoff, Dicke, Lambda) " +
                                           "VALUES (?, 1, ?, 0.015, 1.0)", new DbParam("@a", aufbau), new DbParam("@b", baustoff));
            Assert.Equal(1, KatalogSchluesselSaat.Ausfuehren(null, Katalogfassung.Stufe2));

            Katalogpaket paket = Katalogpaket.AusBytes(Katalogpaket.AusDatenbank(2).Bytes());
            Katalogpaketsatz satz = paket.Tabellen.Single(t => t.Tabelle == "Tab_Bauteilaufbau_STAMM").Saetze.Single(s => s.Schluessel == "BTA:PROBEWAND");
            Assert.Equal("BTA:PROBEWAND", satz.Schluessel);
            Assert.Equal(schluessel, satz.Kinder["Tab_Bauteilschicht_STAMM"].Single()["ID_Baustoff"]);

            DataRepository.ExecuteNonQuery("DELETE FROM Tab_Bauteilschicht_STAMM WHERE ID_Aufbau = " + aufbau);
            DataRepository.ExecuteNonQuery("DELETE FROM Tab_Bauteilaufbau_STAMM WHERE ID = " + aufbau);
            KatalogabgleichErgebnis e = Katalogabgleich.Ausfuehren(paket, nurPruefen: false);
            Assert.Equal((1, 0, 0, 0), (e.Neu, e.Aktualisiert, e.Behalten, e.Ausgelaufen));
            Assert.Equal(baustoff, Zahl("SELECT s.ID_Baustoff FROM Tab_Bauteilschicht_STAMM s JOIN Tab_Bauteilaufbau_STAMM a " +
                                        "ON a.ID = s.ID_Aufbau WHERE a.Katalog_Schluessel = 'BTA:PROBEWAND'"));
        }

        /// <summary>„Auslieferungsstand wiederherstellen" gilt auch für einen Satz der Stufe 2.</summary>
        [Fact]
        public void Wiederherstellen_gilt_auch_fuer_die_Stufe_2()
        {
            if (!_db.Vorhanden) return;

            Katalogpaket paket = Katalogpaket.Lesen(Probe);
            DataRepository.ExecuteNonQuery("UPDATE Tab_Wechselrichter_STAMM SET \"ReadOnly\" = 0, Kosten = 1 WHERE Bezeichner = 'Muster 2500TL'");
            KatalogabgleichErgebnis plan = Katalogabgleich.Ausfuehren(paket, nurPruefen: true);
            KatalogabgleichEintrag z = plan.Eintraege.Single(x => x.Tabelle == "Tab_Wechselrichter_STAMM");
            Assert.Equal(Katalogabgleich.AKTION_BEHALTEN, z.Aktion);
            Assert.True(z.Wiederherstellbar);

            (bool ok, string meldung) = Katalogabgleich.Wiederherstellen(paket, "Tab_Wechselrichter_STAMM", z.Schluessel);
            Assert.True(ok, meldung);
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Wechselrichter_STAMM WHERE Bezeichner = 'Muster 2500TL' AND \"ReadOnly\" = 1 " +
                                  "AND COALESCE(Kosten, -1) <> 1"));
        }

        // =============================================================================
        //  Anbinden: ungesperrt ausgeliefert, später in der Quelle gesperrt
        // =============================================================================

        private const string EINSCHICHT = "Einschicht 5 Tage";
        private const string EINSCHICHT_SCHLUESSEL = "PW:EINSCHICHT_5_TAGE";

        /// <summary>Macht aus dem gesperrten Satz eine Anwenderzeile, wie sie ein ungesperrt ausgelieferter Satz hinterlässt.</summary>
        private static long AlsUngesperrtAusgeliefert(string bezeichner)
        {
            DataRepository.ExecuteNonQuery("UPDATE Tab_Prozesswaerme_STAMM SET \"ReadOnly\" = 0, Katalog_Schluessel = NULL, " +
                                           "Katalog_Pruefsumme = NULL WHERE Bezeichner = ?", new DbParam("@b", bezeichner));
            return Zahl("SELECT ID FROM Tab_Prozesswaerme_STAMM WHERE Bezeichner = '" + bezeichner + "'");
        }

        /// <summary>Die Probe als frisches Paket (veränderbar).</summary>
        private static Katalogpaket ProbeKopie() => Katalogpaket.AusBytes(Katalogpaket.Lesen(Probe).Bytes());

        private static Katalogpaketsatz Satz(Katalogpaket p, string schluessel) =>
            p.Tabellen.Single(t => t.Tabelle == "Tab_Prozesswaerme_STAMM").Saetze.Single(s => s.Schluessel == schluessel);

        /// <summary>Die Folgefassung: der Satz mit einem neuen Wert für <c>Monat_1</c> und neuer Prüfsumme.</summary>
        private static Katalogpaket Folgefassung(double monat1)
        {
            Katalogpaket p = ProbeKopie();
            p.Fassung += 1;
            Katalogpaketsatz s = Satz(p, EINSCHICHT_SCHLUESSEL);
            s.Werte["Monat_1"] = monat1;
            s.Pruefsumme = Neusumme(Pw, s);
            return p;
        }

        private static string Zeilenstand(long id) =>
            Text("SELECT \"ReadOnly\" || '|' || COALESCE(Katalog_Schluessel, '∅') || '|' || COALESCE(Katalog_Pruefsumme, '∅') || '|' || " +
                 "Katalog_Ausgelaufen FROM Tab_Prozesswaerme_STAMM WHERE ID = " + id.ToString(CultureInfo.InvariantCulture));

        /// <summary>
        /// Gleicher Inhalt: Die eine gleichnamige Zeile ohne Schlüssel wird angebunden — ID bleibt, Schlüssel und
        /// Prüfsumme des Pakets, gesperrt; kein Doppel. Die Folgefassung führt die unveränderte Zeile nach.
        /// </summary>
        [Fact]
        public void Anbinden_bei_gleichem_Inhalt_und_die_Folgefassung_fuehrt_nach()
        {
            if (!_db.Vorhanden) return;

            long id = AlsUngesperrtAusgeliefert(EINSCHICHT);
            Katalogpaket paket = ProbeKopie();
            string summe = Satz(paket, EINSCHICHT_SCHLUESSEL).Pruefsumme;
            string projektVorher = Projektbild();

            KatalogabgleichErgebnis plan = Katalogabgleich.Ausfuehren(paket, nurPruefen: true);
            Assert.Equal("0|∅|∅|0", Zeilenstand(id));
            Assert.Equal(Katalogabgleich.AKTION_ANGEBUNDEN, plan.Eintraege.Single().Aktion);

            KatalogabgleichErgebnis e = Katalogabgleich.Ausfuehren(paket, nurPruefen: false);
            Assert.True(e.Ausgefuehrt, e.Meldung);
            Assert.Equal((0, 0, 0, 0, 1), (e.Neu, e.Aktualisiert, e.Behalten, e.Ausgelaufen, e.Angebunden));
            KatalogabgleichEintrag z = e.Eintraege.Single();
            Assert.Equal((Katalogabgleich.AKTION_ANGEBUNDEN, EINSCHICHT_SCHLUESSEL, R.KABG_HINWEIS_ANGEBUNDEN),
                         (z.Aktion, z.Schluessel, z.Hinweis));
            Assert.False(z.Wiederherstellbar);
            Assert.Equal("1|" + EINSCHICHT_SCHLUESSEL + "|" + summe + "|0", Zeilenstand(id));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Prozesswaerme_STAMM WHERE Bezeichner = '" + EINSCHICHT + "'"));
            // Das Protokoll kennt nur die Aktionen seines Schemaschritts: angebunden steht als AKTUALISIERT mit eigenem Hinweis.
            Assert.Contains(Katalogabgleich.Protokoll(), p => p.Aktion == Katalogabgleich.AKTION_AKTUALISIERT &&
                                                              p.Schluessel == EINSCHICHT_SCHLUESSEL &&
                                                              p.Hinweis == R.KABG_HINWEIS_ANGEBUNDEN);
            Assert.EndsWith(string.Format(CultureInfo.CurrentCulture, R.KABG_BERICHT_ANGEBUNDEN, 1), e.Zusammenfassung());
            Assert.Equal(projektVorher, Projektbild());

            // Derselbe Lauf erzwungen: nichts mehr zu tun.
            KatalogabgleichErgebnis erneut = Katalogabgleich.Ausfuehren(paket, nurPruefen: false, erzwingen: true);
            Assert.Empty(erneut.Eintraege);

            // Folgefassung: die angebundene, unveränderte Zeile wird nachgeführt.
            Katalogpaket folge = Folgefassung(4711);
            KatalogabgleichErgebnis f = Katalogabgleich.Ausfuehren(folge, nurPruefen: false);
            Assert.True(f.Ausgefuehrt, f.Meldung);
            Assert.Equal(Katalogabgleich.AKTION_AKTUALISIERT, f.Eintraege.Single().Aktion);
            Assert.Equal(4711.0, Convert.ToDouble(DataRepository.ExecuteScalar(
                "SELECT Monat_1 FROM Tab_Prozesswaerme_STAMM WHERE ID = " + id.ToString(CultureInfo.InvariantCulture)), CultureInfo.InvariantCulture));
            Assert.Equal("1|" + EINSCHICHT_SCHLUESSEL + "|" + Satz(folge, EINSCHICHT_SCHLUESSEL).Pruefsumme + "|0", Zeilenstand(id));
            Assert.Equal(projektVorher, Projektbild());
        }

        /// <summary>
        /// Abweichender Inhalt: angebunden und mit dem Lieferstand überschrieben (ANGEBUNDEN_UEBERSCHRIEBEN) — die
        /// Zeile war nie gesperrt. Dieselbe ID, die Werte des Paketsatzes, gezählt unter „angebunden"; im Protokoll
        /// als AKTUALISIERT mit eigenem Hinweis. Beim Start läuft die Sicherung vorher, auch wenn der Plan nur aus
        /// dieser Anbindung besteht. Die Folgefassung führt die Zeile danach nach.
        /// </summary>
        [Fact]
        public void Anbinden_ueberschreibt_abweichenden_Inhalt_mit_dem_Lieferstand()
        {
            if (!_db.Vorhanden) return;

            Katalogabgleich.StartberichtAbholen();
            long id = AlsUngesperrtAusgeliefert(EINSCHICHT);
            DataRepository.ExecuteNonQuery("UPDATE Tab_Prozesswaerme_STAMM SET Beschreibung = 'eigene Fassung', Monat_1 = 1234 WHERE ID = ?",
                                           new DbParam("@id", id));
            Katalogpaket paket = ProbeKopie();
            string summe = Satz(paket, EINSCHICHT_SCHLUESSEL).Pruefsumme;
            string beschreibung = Convert.ToString(Satz(paket, EINSCHICHT_SCHLUESSEL).Werte["Beschreibung"], CultureInfo.InvariantCulture);
            string projektVorher = Projektbild();

            KatalogabgleichErgebnis plan = Katalogabgleich.Ausfuehren(paket, nurPruefen: true);
            KatalogabgleichEintrag geplant = plan.Eintraege.Single();
            Assert.Equal(Katalogabgleich.AKTION_ANGEBUNDEN_UEBERSCHRIEBEN, geplant.Aktion);
            Assert.False(geplant.Wiederherstellbar);
            Assert.Equal("0|∅|∅|0", Zeilenstand(id));

            string datei = Path.Combine(_db.Ordner, Katalogpaket.DATEINAME);
            paket.Speichern(datei);
            int sicherungen = 0;
            bool sicherungVorDemSchreiben = false;
            KatalogabgleichErgebnis e = Katalogabgleich.BeimStart(datei, () =>
            {
                sicherungen++;
                sicherungVorDemSchreiben = Zeilenstand(id) == "0|∅|∅|0";
                return "sicherung.sqlite";
            });
            Assert.True(e.Ausgefuehrt, e.Meldung);
            Assert.Equal((1, true, "sicherung.sqlite"), (sicherungen, sicherungVorDemSchreiben, e.Sicherung));
            Assert.Equal((0, 0, 0, 0, 1), (e.Neu, e.Aktualisiert, e.Behalten, e.Ausgelaufen, e.Angebunden));
            KatalogabgleichEintrag z = e.Eintraege.Single();
            Assert.Equal((Katalogabgleich.AKTION_ANGEBUNDEN_UEBERSCHRIEBEN, R.KABG_HINWEIS_ANGEBUNDEN_UEBERSCHRIEBEN), (z.Aktion, z.Hinweis));
            Assert.False(z.Wiederherstellbar);
            Assert.Equal("1|" + EINSCHICHT_SCHLUESSEL + "|" + summe + "|0", Zeilenstand(id));
            Assert.Equal(beschreibung, Text("SELECT COALESCE(Beschreibung, '') FROM Tab_Prozesswaerme_STAMM WHERE ID = " + id.ToString(CultureInfo.InvariantCulture)));
            Assert.Equal(summe, Katalogfassung.PruefsummeDerZeile(Pw, Katalogfassung.VorhandeneFachspalten(Pw),
                DataRepository.GetDataTable("SELECT * FROM Tab_Prozesswaerme_STAMM WHERE ID = " + id.ToString(CultureInfo.InvariantCulture)).Rows[0],
                Katalogfassung.LeseOhneVorgang));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Prozesswaerme_STAMM WHERE Bezeichner = '" + EINSCHICHT + "'"));
            Assert.Contains(Katalogabgleich.Protokoll(), p => p.Aktion == Katalogabgleich.AKTION_AKTUALISIERT &&
                                                              p.Schluessel == EINSCHICHT_SCHLUESSEL &&
                                                              p.Hinweis == R.KABG_HINWEIS_ANGEBUNDEN_UEBERSCHRIEBEN);
            Assert.DoesNotContain(Katalogabgleich.Protokoll(), p => p.Aktion == Katalogabgleich.AKTION_BEHALTEN);
            Assert.Contains("sicherung.sqlite", Katalogabgleich.StartberichtAbholen().Starttext());
            Assert.Equal(projektVorher, Projektbild());

            // Folgefassung: die angebundene Zeile entspricht dem Lieferstand und wird nachgeführt.
            Katalogpaket folge = Folgefassung(4711);
            KatalogabgleichErgebnis f = Katalogabgleich.Ausfuehren(folge, nurPruefen: false);
            Assert.True(f.Ausgefuehrt, f.Meldung);
            Assert.Equal(Katalogabgleich.AKTION_AKTUALISIERT, f.Eintraege.Single().Aktion);
            Assert.Equal(4711.0, Convert.ToDouble(DataRepository.ExecuteScalar(
                "SELECT Monat_1 FROM Tab_Prozesswaerme_STAMM WHERE ID = " + id.ToString(CultureInfo.InvariantCulture)), CultureInfo.InvariantCulture));
            Assert.Equal("1|" + EINSCHICHT_SCHLUESSEL + "|" + Satz(folge, EINSCHICHT_SCHLUESSEL).Pruefsumme + "|0", Zeilenstand(id));
            Assert.Equal(projektVorher, Projektbild());
        }

        /// <summary>
        /// Überschreiben beim Anbinden führt die Kindzeilen nach wie AKTUALISIERT: Der Baustoff behält seine ID, die
        /// eigenen Synonyme des Anwenders weichen den gelieferten, die gesperrt eingefügt werden.
        /// </summary>
        [Fact]
        public void Anbinden_mit_Ueberschreiben_fuehrt_die_Kindzeilen_nach()
        {
            if (!_db.Vorhanden) return;

            const string baustoff = "Leichtputz 1000";
            long id = Zahl("SELECT ID FROM Tab_Baustoff_STAMM WHERE Bezeichner = '" + baustoff + "'");
            Katalogpaket paket = Katalogpaket.AusBytes(Katalogpaket.AusDatenbank(2).Bytes());
            Katalogpaketsatz satz = paket.Tabellen.Single(t => t.Tabelle == "Tab_Baustoff_STAMM").Saetze.Single(s => s.Bezeichner == baustoff);
            int synonyme = satz.Kinder["Tab_Baustoffsynonym_STAMM"].Count;
            Assert.True(synonyme > 0);
            string synonymeVorher = Text("SELECT group_concat(Materialname || '/' || Sprache, ';') FROM (SELECT Materialname, Sprache " +
                                         "FROM Tab_Baustoffsynonym_STAMM WHERE ID_Baustoff = " + id + " ORDER BY Materialname, Sprache)");

            // Ungesperrt ausgeliefert und vom Anwender gepflegt: ohne Schlüssel, eigener λ-Wert, eigenes Synonym.
            DataRepository.ExecuteNonQuery("UPDATE Tab_Baustoff_STAMM SET \"ReadOnly\" = 0, Katalog_Schluessel = NULL, Katalog_Pruefsumme = NULL, " +
                                           "Lambda = 0.99 WHERE ID = ?", new DbParam("@id", id));
            DataRepository.ExecuteNonQuery("DELETE FROM Tab_Baustoffsynonym_STAMM WHERE ID_Baustoff = ?", new DbParam("@id", id));
            DataRepository.ExecuteNonQuery("INSERT INTO Tab_Baustoffsynonym_STAMM (Materialname, Sprache, ID_Baustoff, \"ReadOnly\") " +
                                           "VALUES ('eigener Putz', 'de', ?, 0)", new DbParam("@id", id));

            KatalogabgleichErgebnis e = Katalogabgleich.Ausfuehren(paket, nurPruefen: false, erzwingen: true);
            Assert.True(e.Ausgefuehrt, e.Meldung);
            KatalogabgleichEintrag z = e.Eintraege.Single(x => x.Schluessel == satz.Schluessel);
            Assert.Equal(Katalogabgleich.AKTION_ANGEBUNDEN_UEBERSCHRIEBEN, z.Aktion);
            Assert.Equal(1, e.Angebunden);
            Assert.Equal(id, Zahl("SELECT ID FROM Tab_Baustoff_STAMM WHERE Katalog_Schluessel = '" + satz.Schluessel + "'"));
            Assert.Equal(1L, Zahl("SELECT \"ReadOnly\" FROM Tab_Baustoff_STAMM WHERE ID = " + id));
            Assert.Equal(satz.Pruefsumme, Text("SELECT Katalog_Pruefsumme FROM Tab_Baustoff_STAMM WHERE ID = " + id));
            Assert.Equal(Convert.ToDouble(satz.Werte["Lambda"], CultureInfo.InvariantCulture),
                         Convert.ToDouble(DataRepository.ExecuteScalar("SELECT Lambda FROM Tab_Baustoff_STAMM WHERE ID = " + id), CultureInfo.InvariantCulture));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Baustoffsynonym_STAMM WHERE Materialname = 'eigener Putz'"));
            Assert.Equal((long)synonyme, Zahl("SELECT COUNT(*) FROM Tab_Baustoffsynonym_STAMM WHERE ID_Baustoff = " + id + " AND \"ReadOnly\" = 1"));
            Assert.Equal(synonymeVorher, Text("SELECT group_concat(Materialname || '/' || Sprache, ';') FROM (SELECT Materialname, Sprache " +
                                              "FROM Tab_Baustoffsynonym_STAMM WHERE ID_Baustoff = " + id + " ORDER BY Materialname, Sprache)"));
        }

        /// <summary>Zwei gleichnamige Zeilen ohne Schlüssel (Name ohne Unterschied der Schreibung): kein Anbinden.</summary>
        [Fact]
        public void Kein_Anbinden_bei_zwei_gleichnamigen_Zeilen()
        {
            if (!_db.Vorhanden) return;

            long id = AlsUngesperrtAusgeliefert(EINSCHICHT);
            DataRepository.ExecuteNonQuery("INSERT INTO Tab_Prozesswaerme_STAMM (Bezeichner, \"ReadOnly\", Katalog_Ausgelaufen) " +
                                           "VALUES (?, 0, 0)", new DbParam("@b", EINSCHICHT.ToLowerInvariant()));
            KatalogabgleichErgebnis e = Katalogabgleich.Ausfuehren(ProbeKopie(), nurPruefen: false);
            Assert.True(e.Ausgefuehrt, e.Meldung);
            KatalogabgleichEintrag z = e.Eintraege.Single();
            Assert.Equal((Katalogabgleich.AKTION_BEHALTEN, 0, 1),
                         (z.Aktion, e.Angebunden, e.Behalten));
            Assert.Equal(string.Format(CultureInfo.CurrentCulture, R.KABG_HINWEIS_NAME_MEHRDEUTIG, EINSCHICHT), z.Hinweis);
            Assert.False(z.Wiederherstellbar);
            Assert.Equal("0|∅|∅|0", Zeilenstand(id));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Prozesswaerme_STAMM WHERE lower(Bezeichner) = lower('" + EINSCHICHT +
                                  "') AND Katalog_Schluessel IS NOT NULL"));
            (bool ok, _) = Katalogabgleich.Wiederherstellen(ProbeKopie(), "Tab_Prozesswaerme_STAMM", EINSCHICHT_SCHLUESSEL);
            Assert.False(ok);
        }

        /// <summary>Die gleichnamige Zeile trägt schon einen anderen Schlüssel: kein Anbinden, eigener Hinweis.</summary>
        [Fact]
        public void Kein_Anbinden_bei_fremdem_Schluessel()
        {
            if (!_db.Vorhanden) return;

            long id = AlsUngesperrtAusgeliefert(EINSCHICHT);
            DataRepository.ExecuteNonQuery("UPDATE Tab_Prozesswaerme_STAMM SET Katalog_Schluessel = 'PW:ANDERER_SATZ' WHERE ID = ?",
                                           new DbParam("@id", id));

            KatalogabgleichErgebnis e = Katalogabgleich.Ausfuehren(ProbeKopie(), nurPruefen: false);
            Assert.True(e.Ausgefuehrt, e.Meldung);
            KatalogabgleichEintrag z = e.Eintraege.Single(x => x.Schluessel == EINSCHICHT_SCHLUESSEL);
            Assert.Equal(Katalogabgleich.AKTION_BEHALTEN, z.Aktion);
            Assert.Equal(string.Format(CultureInfo.CurrentCulture, R.KABG_HINWEIS_NAME_FREMDER_SCHLUESSEL, EINSCHICHT), z.Hinweis);
            Assert.Equal(0, e.Angebunden);
            Assert.Equal("0|PW:ANDERER_SATZ|∅|1", Zeilenstand(id));   // der fremde Schlüssel läuft aus, wie jeder Satz ohne Paketzeile
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Prozesswaerme_STAMM WHERE Bezeichner = '" + EINSCHICHT + "'"));
        }

        private static string Neusumme(Katalogtabelle t, Katalogpaketsatz s) =>
            Katalogfassung.Pruefsumme(t, s.Werte, s.Kinder.ToDictionary(k => k.Key,
                k => (IReadOnlyList<IReadOnlyDictionary<string, object>>)k.Value.Cast<IReadOnlyDictionary<string, object>>().ToList()));

        private static string Tabellenbild2(string sql)
        {
            DataTable dt = DataRepository.GetDataTable(sql);
            return string.Join("|", dt.Rows.Cast<DataRow>().Select(r => Convert.ToString(r[0], CultureInfo.InvariantCulture)));
        }

        // =============================================================================
        //  Hilfen
        // =============================================================================

        private static long Zahl(string sql) => Convert.ToInt64(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);

        private static string Text(string sql) => Convert.ToString(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);

        private static Dictionary<string, string> Schluesselbild()
        {
            var d = new Dictionary<string, string>();
            foreach (Katalogtabelle t in Katalogfassung.Alle)
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
