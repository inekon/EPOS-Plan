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
    /// <b>Das Katalogpaket neben der Vorlage</b> (Entscheidungsvorlage Modellgrenzen KU1 Stufe 1 und 2) —
    /// am Ergebnis desselben Laufs wie <see cref="VorlageTests"/>.
    ///
    /// <para><b>Die Probe</b> <c>Referenzlaeufe/Importproben/Katalogpaket_Probe.json</c> ist der
    /// Ausschnitt des Pakets der Testdatenbank (Prozesswärme-Betriebsweisen samt Wochenprofilen aus der Stufe 1,
    /// Konditionierungsvorlagen samt Perioden und der Muster-Wechselrichter aus der Stufe 2, Fassung 1) — neutrale Namen, runde Werte, kein Herstellerdatum.
    /// Sie muss mit dem Teil des geschriebenen Pakets byte-gleich sein; dieselbe Probe nehmen die
    /// Abgleichsfälle in <c>EPOS.Kern.Tests/KatalogabgleichTests</c>.</para>
    /// </summary>
    [Collection("Auslieferungsvorlage")]
    public sealed class KatalogpaketVorlageTests : IClassFixture<Vorlage>
    {
        /// <summary>Die Tabellen der Probe.</summary>
        internal static readonly string[] PROBENTABELLEN =
        {
            "Tab_Prozesswaerme_STAMM", "Tab_Prozesstyp_STAMM", "Tab_Konditionierungsvorlage_STAMM", "Tab_Wechselrichter_STAMM"
        };

        private readonly Vorlage _v;
        public KatalogpaketVorlageTests(Vorlage v) { _v = v; }

        private string Paketdatei => Katalogpaket.Pfad(_v.Ziel);

        private static string Probe => Path.Combine(Werkzeuglauf.Repowurzel, "Referenzlaeufe", "Importproben",
                                                    "Katalogpaket_Probe.json");

        /// <summary>Das Paket entsteht neben der Vorlage, ist lesbar und in seiner kanonischen Form geschrieben.</summary>
        [Fact]
        public void K1_Das_Paket_liegt_neben_der_Vorlage_und_ist_kanonisch()
        {
            if (!_v.Vorhanden || _v.Lauf.Code != 0) return;

            Assert.True(File.Exists(Paketdatei), "Das Katalogpaket fehlt neben der Vorlage: " + Paketdatei);
            byte[] bytes = File.ReadAllBytes(Paketdatei);
            Katalogpaket paket = Katalogpaket.AusBytes(bytes);
            Assert.True(paket.Fassung >= 1);
            Assert.Equal(bytes, paket.Bytes());
            Assert.Contains("Schritt 4b — Katalogpaket", File.ReadAllText(_v.Ziel + ".bericht.txt"));
        }

        /// <summary>Der Probenteil des Pakets (Stufe 1 und 2) ist byte-gleich mit der Probe im Repositorium.</summary>
        [Fact]
        public void K2_Der_Prozesswaermeteil_ist_byte_gleich_mit_der_Probe()
        {
            if (!_v.Vorhanden || _v.Lauf.Code != 0) return;

            Katalogpaket paket = Katalogpaket.Lesen(Paketdatei);
            var teil = new Katalogpaket { Fassung = 1 };
            teil.Tabellen.AddRange(paket.Tabellen.Where(t => PROBENTABELLEN.Contains(t.Tabelle)));
            Assert.Equal(PROBENTABELLEN.Length, teil.Tabellen.Count);
            Assert.Equal(File.ReadAllBytes(Probe), teil.Bytes());
        }

        /// <summary>
        /// Die Vorlage trägt genau den Stand des Pakets: dieselbe Katalogfassung, und jeder gesperrte
        /// Satz des Registers mit Schlüssel und der Prüfsumme des Pakets. Eine Neuinstallation gleicht
        /// deshalb beim ersten Start nichts ab.
        /// </summary>
        [Fact]
        public void K3_Die_Vorlage_traegt_Fassung_Schluessel_und_Pruefsummen_des_Pakets()
        {
            if (!_v.Vorhanden || _v.Lauf.Code != 0) return;

            Katalogpaket paket = Katalogpaket.Lesen(Paketdatei);
            var befund = _v.Lesen(() =>
            {
                int? fassung = Katalogabgleich.FassungDerDatenbank();
                KatalogabgleichErgebnis plan = Katalogabgleich.Ausfuehren(paket, nurPruefen: true, erzwingen: true);
                var ohneSchluessel = new List<string>();
                foreach (Katalogtabelle t in Katalogfassung.Alle)
                {
                    object n = DataRepository.ExecuteScalar("SELECT COUNT(*) FROM \"" + t.Tabelle +
                                                            "\" WHERE \"ReadOnly\" = 1 AND \"Katalog_Schluessel\" IS NULL");
                    if (Convert.ToInt64(n) > 0) ohneSchluessel.Add(t.Tabelle);
                }
                return (Fassung: fassung, Plan: plan, Ohne: ohneSchluessel);
            });

            Assert.Equal(paket.Fassung, befund.Fassung);
            Assert.Empty(befund.Ohne);
            Assert.Equal(0, befund.Plan.Neu + befund.Plan.Aktualisiert + befund.Plan.Behalten + befund.Plan.Ausgelaufen);
        }

        // ---- Konzept Setup 6.5.3, Schritte 2 und 3; Entscheide E2, E3, E7 ----------------------

        /// <summary>
        /// <b>Leerer Paketteil bricht ab (E2).</b> Gegen die Testdatenbank melden Heizkessel und PV (und die übrigen
        /// Registerkataloge ohne gesperrten Satz) den leeren Paketteil: Ohne benannte Ausnahme bricht das Werkzeug mit
        /// Code 6 ab und nennt genau die nicht ausgenommenen Kataloge. Mit den Ausnahmen läuft es durch (der Lauf der
        /// Vorrichtung), und der Bericht weist jede Ausnahme und die ReadOnly-Bilanz aus. Ein Name außerhalb des
        /// Registers ist ein Aufruffehler.
        /// </summary>
        [Fact]
        public void K4_Leerer_Paketteil_bricht_ab_und_die_benannte_Ausnahme_steht_im_Bericht()
        {
            if (!_v.Vorhanden) return;

            using var o = new Arbeitsordner();
            string[] ausserPv = Werkzeuglauf.LEERE_PAKETTEILE_DER_TESTDATENBANK.Where(t => t != "Tab_PV_STAMM")
                                            .SelectMany(t => new[] { "--ohne-paket", t }).ToArray();
            Werkzeuglauf.Ergebnis e = Werkzeuglauf.Starten(
                new[] { _v.Quelle, o.Datei("Kenndaten.sqlite"), "--trocken" }.Concat(ausserPv).ToArray());
            Assert.True(e.Code == 6, e.Alles);
            Assert.Contains("Leerer Paketteil in 1 Registerkatalog(en) mit Zeilen: Tab_PV_STAMM.", e.Fehlerausgabe);
            Assert.Contains("FEHLER  leerer Paketteil Tab_PV_STAMM", e.Ausgabe);
            Assert.DoesNotContain("FEHLER  leerer Paketteil Tab_Heizkessel_STAMM", e.Ausgabe);
            Assert.False(File.Exists(o.Datei("Kenndaten.sqlite")));

            Werkzeuglauf.Ergebnis ohne = Werkzeuglauf.Starten(_v.Quelle, o.Datei("Kenndaten.sqlite"), "--trocken");
            Assert.True(ohne.Code == 6, ohne.Alles);
            foreach (string t in Werkzeuglauf.LEERE_PAKETTEILE_DER_TESTDATENBANK)
                Assert.Contains("FEHLER  leerer Paketteil " + t + ":", ohne.Ausgabe);

            Werkzeuglauf.Ergebnis fremd = Werkzeuglauf.Starten(_v.Quelle, o.Datei("Kenndaten.sqlite"), "--trocken",
                                                               "--ohne-paket", "Tab_Gibtsnicht_STAMM");
            Assert.True(fremd.Code == 2, fremd.Alles);
            Assert.Contains("Tab_Gibtsnicht_STAMM ist kein Katalog des Registers", fremd.Fehlerausgabe);

            if (_v.Lauf.Code != 0) return;
            string bericht = File.ReadAllText(_v.Ziel + ".bericht.txt");
            Assert.Contains("ReadOnly-Bilanz je Registerkatalog: Zeilen / gesperrt mit Schluessel (im Paket) / ungesperrt", bericht);
            Assert.Matches(@"  Tab_Heizkessel_STAMM +\d+ / 0 / \d+   leerer Paketteil, benannte Ausnahme", bericht);
            foreach (string t in Werkzeuglauf.LEERE_PAKETTEILE_DER_TESTDATENBANK)
                Assert.Contains("WARNUNG leerer Paketteil " + t + " - benannte Ausnahme --ohne-paket", bericht);
            Assert.Matches(@"WARNUNG \d+ ungesperrte Zeile\(n\) in \d+ Registerkatalog\(en\) gehen im Modus alle ohne Schluessel", bericht);
        }

        /// <summary>
        /// <b>Eine Quelle mit gesperrten Sätzen ergibt keine Warnung (Schritt 2, E7).</b> Sind alle Zeilen der
        /// Registerkataloge gesperrt, gibt es weder einen leeren Paketteil noch ungesperrte Zeilen: Der Lauf geht ohne
        /// Ausnahme durch, die Bilanz zeigt „n / n / 0“, und weder die Warnung zum leeren Paketteil noch die
        /// Sammelwarnung steht im Bericht.
        /// </summary>
        [Fact]
        public void K5_Eine_Quelle_mit_gesperrten_Saetzen_ergibt_keine_Warnung()
        {
            if (!_v.Vorhanden) return;

            using var o = new Arbeitsordner();
            string quelle = o.Datei("quelle.sqlite");
            File.Copy(Werkzeuglauf.Testdatenbank, quelle);
            Aendern(quelle, Katalogfassung.Alle.Select(t => "UPDATE \"" + t.Tabelle + "\" SET \"ReadOnly\" = 1").ToArray());

            Werkzeuglauf.Ergebnis e = Werkzeuglauf.Starten(quelle, o.Datei("Kenndaten.sqlite"), "--trocken");
            Assert.True(e.Code == 0, e.Alles);
            Assert.Matches(@"  Tab_Heizkessel_STAMM +(\d+) / \1 / 0\r?\n", e.Ausgabe);
            Assert.Matches(@"  Tab_PV_STAMM +(\d+) / \1 / 0\r?\n", e.Ausgabe);
            Assert.DoesNotContain("leerer Paketteil", e.Ausgabe);
            Assert.DoesNotContain("ungesperrte Zeile", e.Ausgabe);
        }

        /// <summary>
        /// <b>Paket und Vorlage gehören zusammen (Schritt 3).</b> Das Werkzeug liest das geschriebene Paket zurück und
        /// meldet den Abgleich gegen die Vorlage im Bericht. Derselbe Vergleich des Kerns
        /// (<see cref="Katalogpaket.Abweichungen"/>) findet am Ergebnis nichts und meldet jede Störung: eine andere
        /// Fassung, einen fehlenden Satz, eine abweichende Prüfsumme und eine fehlende Tabelle.
        /// </summary>
        [Fact]
        public void K6_Das_zurueckgelesene_Paket_passt_zur_Vorlage_und_jede_Abweichung_faellt_auf()
        {
            if (!_v.Vorhanden || _v.Lauf.Code != 0) return;

            Assert.Contains("ok      Paket zurueckgelesen: Fassung ", File.ReadAllText(_v.Ziel + ".bericht.txt"));

            var stand = _v.Lesen(() => (Fassung: Katalogabgleich.FassungDerDatenbank(), Saetze: Katalogpaket.GesperrterStand()));
            Assert.Empty(Katalogpaket.Abweichungen(Katalogpaket.Lesen(Paketdatei), stand.Fassung, stand.Saetze));

            Katalogpaket fassung = Katalogpaket.Lesen(Paketdatei);
            fassung.Fassung++;
            Assert.Contains(Katalogpaket.Abweichungen(fassung, stand.Fassung, stand.Saetze), a => a.StartsWith("Fassung: "));

            Katalogpaket weniger = Katalogpaket.Lesen(Paketdatei);
            Katalogpakettabelle bhkw = weniger.Tabellen.Single(t => t.Tabelle == "Tab_BHKW_STAMM");
            Assert.NotEmpty(bhkw.Saetze);
            bhkw.Saetze.RemoveAt(0);
            Assert.Contains(Katalogpaket.Abweichungen(weniger, stand.Fassung, stand.Saetze),
                            a => a.StartsWith("Tab_BHKW_STAMM: ") && a.Contains(" Satz/Saetze im Paket, "));

            Katalogpaket summe = Katalogpaket.Lesen(Paketdatei);
            summe.Tabellen.Single(t => t.Tabelle == "Tab_BHKW_STAMM").Saetze[0].Pruefsumme = "0000";
            Assert.Contains(Katalogpaket.Abweichungen(summe, stand.Fassung, stand.Saetze),
                            a => a.StartsWith("Tab_BHKW_STAMM: Pruefsumme von "));

            Katalogpaket ohneTabelle = Katalogpaket.Lesen(Paketdatei);
            ohneTabelle.Tabellen.RemoveAll(t => t.Tabelle == "Tab_BHKW_STAMM");
            Assert.Contains(Katalogpaket.Abweichungen(ohneTabelle, stand.Fassung, stand.Saetze),
                            a => a.StartsWith("Tab_BHKW_STAMM: fehlt im Paket"));
        }

        /// <summary>
        /// <b>Quelle mit älterem Schemastand bricht ab (E3).</b> Das Werkzeug migriert nicht; eine Quelle unter
        /// <see cref="SchemaStand.Zielversion"/> bricht mit Code 8 ab und nennt beide Stände.
        /// </summary>
        [Fact]
        public void K7_Eine_Quelle_mit_aelterem_Schemastand_bricht_ab()
        {
            if (!_v.Vorhanden) return;

            using var o = new Arbeitsordner();
            string quelle = o.Datei("quelle.sqlite");
            File.Copy(Werkzeuglauf.Testdatenbank, quelle);
            int aelter = SchemaStand.Zielversion - 1;
            Aendern(quelle, "UPDATE \"Tab_Applikation\" SET \"SchemaVersion\" = " + aelter);

            Werkzeuglauf.Ergebnis e = Werkzeuglauf.StartenMitAusnahmen(quelle, o.Datei("Kenndaten.sqlite"), "--trocken");
            Assert.True(e.Code == 8, e.Alles);
            Assert.Contains("Der Schemastand der Quelle (" + aelter + ") ist aelter als der Zielstand der Schemakette (" +
                            SchemaStand.Zielversion + ")", e.Fehlerausgabe);
            Assert.DoesNotContain("Schritt 2", e.Ausgabe);
        }

        /// <summary>Führt SQL-Texte auf einer Kopie aus, ohne Pool (die Datei wird danach vom Werkzeug geöffnet).</summary>
        private static void Aendern(string datei, params string[] sql)
        {
            using (var c = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=" + datei + ";Pooling=False"))
            {
                c.Open();
                foreach (string s in sql)
                    using (var k = c.CreateCommand())
                    {
                        k.CommandText = s;
                        k.ExecuteNonQuery();
                    }
            }
        }
    }
}
