using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace Auslieferungsvorlage.Tests
{
    /// <summary>
    /// <b>Zwei Fassungen des Katalogpakets aus der Testdatenbank</b> — die Vorrichtung des Ende-zu-Ende-Nachweises
    /// (Konzept Setup 6.5.3, Schritt 5). Sie baut mit dem Werkzeug, wie es die Setup-Kette aufruft:
    /// <list type="bullet">
    ///   <item><b>Fassung n</b> aus einer unveränderten Kopie der Testdatenbank;</item>
    ///   <item><b>Fassung n + 1</b> aus einer Kopie, in der zwei gesperrte BHKW-Sätze (A, B) eine andere
    ///   elektrische Leistung tragen und ein im Modus <c>alle</c> ungesperrt ausgelieferter Heizkessel (D) gesperrt
    ///   ist;</item>
    ///   <item><b>Fassung n + 2</b> wie n + 1, dazu trägt der Heizkessel D eine andere thermische Leistung.</item>
    /// </list>
    /// Beide Läufe im Modus <c>alle</c> mit den benannten Ausnahmen der Testdatenbank
    /// (<see cref="Werkzeuglauf.LEERE_PAKETTEILE_DER_TESTDATENBANK"/>). Die Testdatenbank selbst wird nie geöffnet.
    /// </summary>
    public sealed class Katalogupdate : IDisposable
    {
        internal const int FASSUNG_N = 2026100601;
        internal const int FASSUNG_N1 = 2026100602;
        internal const int FASSUNG_N2 = 2026100603;

        private readonly Arbeitsordner _ordner = new Arbeitsordner();

        public Katalogupdate()
        {
            Vorhanden = Werkzeuglauf.Testdatenbank != null;
            if (!Vorhanden) return;

            string quelleN = _ordner.Datei("quelle_n.sqlite");
            File.Copy(Werkzeuglauf.Testdatenbank, quelleN);
            Directory.CreateDirectory(_ordner.Datei("n"));
            VorlageN = Path.Combine(_ordner.Datei("n"), "Kenndaten.sqlite");
            LaufN = Werkzeuglauf.StartenMitAusnahmen(quelleN, VorlageN,
                                                     "--katalogfassung", FASSUNG_N.ToString(CultureInfo.InvariantCulture));
            if (LaufN.Code != 0) return;

            // Die Sätze des Nachweises, bestimmt an der Vorlage n: A und B sind die ersten beiden gesperrten
            // BHKW-Sätze (über ihren Schlüssel), D der erste ungesperrte Heizkessel (über seinen Namen).
            SchluesselA = Wert(VorlageN, "SELECT \"Katalog_Schluessel\" FROM \"Tab_BHKW_STAMM\" WHERE \"ReadOnly\" = 1 " +
                                         "ORDER BY \"ID\" LIMIT 1");
            SchluesselB = Wert(VorlageN, "SELECT \"Katalog_Schluessel\" FROM \"Tab_BHKW_STAMM\" WHERE \"ReadOnly\" = 1 " +
                                         "ORDER BY \"ID\" LIMIT 1 OFFSET 1");
            NameD = Wert(VorlageN, "SELECT \"Bezeichner\" FROM \"Tab_Heizkessel_STAMM\" WHERE \"ReadOnly\" = 0 " +
                                   "ORDER BY \"ID\" LIMIT 1");

            string quelleN1 = _ordner.Datei("quelle_n1.sqlite");
            File.Copy(Werkzeuglauf.Testdatenbank, quelleN1);
            Ausfuehren(quelleN1,
                "UPDATE \"Tab_BHKW_STAMM\" SET \"Pel\" = \"Pel\" + 1 WHERE \"Katalog_Schluessel\" IN ('" + SchluesselA + "', '" +
                SchluesselB + "')",
                "UPDATE \"Tab_Heizkessel_STAMM\" SET \"ReadOnly\" = 1 WHERE \"ID\" = (SELECT MIN(\"ID\") FROM " +
                "\"Tab_Heizkessel_STAMM\" WHERE \"ReadOnly\" = 0)");
            Directory.CreateDirectory(_ordner.Datei("n1"));
            VorlageN1 = Path.Combine(_ordner.Datei("n1"), "Kenndaten.sqlite");
            LaufN1 = Werkzeuglauf.StartenMitAusnahmen(quelleN1, VorlageN1,
                                                      "--katalogfassung", FASSUNG_N1.ToString(CultureInfo.InvariantCulture));
            if (LaufN1.Code != 0) return;

            // Fassung n + 2: der Stand von n + 1, dazu eine neue Leistung am nun gesperrten Heizkessel D.
            string quelleN2 = _ordner.Datei("quelle_n2.sqlite");
            File.Copy(quelleN1, quelleN2);
            Ausfuehren(quelleN2,
                "UPDATE \"Tab_Heizkessel_STAMM\" SET \"Ptherm\" = COALESCE(\"Ptherm\", 0) + 1 WHERE \"Bezeichner\" = '" +
                NameD.Replace("'", "''") + "'");
            Directory.CreateDirectory(_ordner.Datei("n2"));
            VorlageN2 = Path.Combine(_ordner.Datei("n2"), "Kenndaten.sqlite");
            LaufN2 = Werkzeuglauf.StartenMitAusnahmen(quelleN2, VorlageN2,
                                                      "--katalogfassung", FASSUNG_N2.ToString(CultureInfo.InvariantCulture));
        }

        internal bool Vorhanden { get; }
        internal string VorlageN { get; }
        internal string VorlageN1 { get; }
        internal string VorlageN2 { get; }
        internal Werkzeuglauf.Ergebnis LaufN { get; }
        internal Werkzeuglauf.Ergebnis LaufN1 { get; }
        internal Werkzeuglauf.Ergebnis LaufN2 { get; }
        internal string SchluesselA { get; }
        internal string SchluesselB { get; }
        internal string NameD { get; }

        /// <summary>Alle drei Fassungen sind gebaut.</summary>
        internal bool Bereit => Vorhanden && LaufN?.Code == 0 && LaufN1?.Code == 0 && LaufN2?.Code == 0;

        internal string PaketN => Katalogpaket.Pfad(VorlageN);
        internal string PaketN1 => Katalogpaket.Pfad(VorlageN1);
        internal string PaketN2 => Katalogpaket.Pfad(VorlageN2);

        /// <summary>Ein neuer Datenordner mit Sicherungsordner, wie ihn die Anwendung neben der Datenbank führt.</summary>
        internal string NeuerDatenordner()
        {
            string ordner = _ordner.Datei("anwender-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(Path.Combine(ordner, "DB-Backup"));
            return ordner;
        }

        /// <summary>Führt SQL-Texte auf einer Datei aus, ohne Pool.</summary>
        internal static void Ausfuehren(string datei, params string[] sql)
        {
            using var c = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=" + datei + ";Pooling=False");
            c.Open();
            foreach (string s in sql)
            {
                using var k = c.CreateCommand();
                k.CommandText = s;
                k.ExecuteNonQuery();
            }
        }

        /// <summary>Der erste Wert einer Abfrage als Text (<c>null</c> bei leerem Ergebnis).</summary>
        internal static string Wert(string datei, string sql) => Zeilen(datei, sql).FirstOrDefault();

        /// <summary>Jede Ergebniszeile als ein Text aus allen Spalten (Trenner „|“), ohne Pool.</summary>
        internal static List<string> Zeilen(string datei, string sql)
        {
            var liste = new List<string>();
            using var c = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=" + datei + ";Mode=ReadOnly;Pooling=False");
            c.Open();
            using var k = c.CreateCommand();
            k.CommandText = sql;
            using var r = k.ExecuteReader();
            while (r.Read())
            {
                var teile = new string[r.FieldCount];
                for (int i = 0; i < r.FieldCount; i++)
                    teile[i] = r.IsDBNull(i) ? "∅" : Convert.ToString(r.GetValue(i), CultureInfo.InvariantCulture);
                liste.Add(string.Join("|", teile));
            }
            return liste;
        }

        public void Dispose() => _ordner.Dispose();
    }

    /// <summary>
    /// <b>Ende-zu-Ende-Nachweis Neuinstallation und Update</b> (Konzept Setup 6.5.3, Schritt 5) — ohne Inno Setup
    /// und ohne Windows. Das Werkzeug baut Vorlage und Paket zweier Fassungen (<see cref="Katalogupdate"/>); der
    /// Startweg des Kerns läuft auf einer Anwenderkopie wie in <c>Program.Main</c>: <see cref="Erstbereitstellung"/>,
    /// Schemastand, dann <see cref="Katalogabgleich.BeimStart"/> mit dem Paket neben der Vorlage
    /// (<see cref="Katalogpaket.Pfad"/>) und der Sicherung <see cref="Katalogabgleich.SicherungAnlegen"/>.
    ///
    /// <para><b>Schemamigration.</b> <c>SchemaMigration.Ausfuehren</c> liegt in der Windows-Schale und ist von hier
    /// nicht erreichbar. Das Werkzeug baut nur aus einer Quelle auf dem Zielstand der Schemakette (Code 8); die
    /// Probe hält deshalb fest, dass die bereitgestellte Kopie auf <see cref="SchemaStand.Zielversion"/> steht —
    /// der Stand, an dem die Migration keinen Schritt hat.</para>
    /// </summary>
    [Collection("Auslieferungsvorlage")]
    public sealed class KatalogpaketUpdateTests : IClassFixture<Katalogupdate>
    {
        private readonly Katalogupdate _k;
        public KatalogpaketUpdateTests(Katalogupdate k) { _k = k; }

        /// <summary>Bereitstellen wie beim ersten Start und den Abgleich des Starts ziehen.</summary>
        private static string Bereitstellen(string datenordner, string vorlage)
        {
            string ziel = Path.Combine(datenordner, "Kenndaten.sqlite");
            Erstbereitstellungsergebnis b = Erstbereitstellung.Sicherstellen(ziel, vorlage);
            Assert.Equal(Erstbereitstellungslage.Kopiert, b.Lage);
            return ziel;
        }

        /// <summary>Führt <paramref name="schritt"/> gegen <paramref name="datei"/> aus, mit Schreibfreigabe wie im Programm.</summary>
        private static T Gegen<T>(string datei, Func<T> schritt)
        {
            string vorher = DataRepository.PfadUeberschreibung;
            Func<bool> schreibrecht = Schreibnaht.Schreibrecht;
            try
            {
                DataRepository.PfadUeberschreibung = datei;
                Schreibnaht.WerkzeugFreigabe("Auslieferungsvorlage.Tests (Katalogpaket-Update)");
                return schritt();
            }
            finally
            {
                DataRepository.PfadUeberschreibung = vorher;
                Schreibnaht.Schreibrecht = schreibrecht;
                Katalogabgleich.StartberichtAbholen();
                try { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); } catch { }
            }
        }

        [Fact]
        public void U0_Beide_Fassungen_entstehen_mit_Paket()
        {
            if (!_k.Vorhanden) return;
            Assert.True(_k.LaufN.Code == 0, _k.LaufN.Alles);
            Assert.True(_k.LaufN1.Code == 0, _k.LaufN1.Alles);
            Assert.Equal(Katalogupdate.FASSUNG_N, Katalogpaket.Lesen(_k.PaketN).Fassung);
            Assert.Equal(Katalogupdate.FASSUNG_N1, Katalogpaket.Lesen(_k.PaketN1).Fassung);
            Assert.False(string.IsNullOrEmpty(_k.SchluesselA));
            Assert.False(string.IsNullOrEmpty(_k.SchluesselB));
            Assert.False(string.IsNullOrEmpty(_k.NameD));
        }

        /// <summary>
        /// <b>Neuinstallation (Freigabeprobe 7 a):</b> Die Vorlage wird bereitgestellt, steht auf dem Zielstand der
        /// Schemakette und auf Fassung n; der Abgleich beim Start tut nichts und legt keine Sicherung an, und der
        /// gesperrte Stand der Datenbank ist genau der des Pakets.
        /// </summary>
        [Fact]
        public void U1_Neuinstallation_gleicht_nicht_ab()
        {
            if (!_k.Bereit) return;

            string db = Bereitstellen(_k.NeuerDatenordner(), _k.VorlageN);
            int sicherungen = 0;
            var befund = Gegen(db, () =>
            {
                KatalogabgleichErgebnis e = Katalogabgleich.BeimStart(Katalogpaket.Pfad(_k.VorlageN),
                                                                      () => { sicherungen++; return ""; });
                int? fassung = Katalogabgleich.FassungDerDatenbank();
                var abweichungen = Katalogpaket.Abweichungen(Katalogpaket.Lesen(_k.PaketN), fassung,
                                                             Katalogpaket.GesperrterStand());
                return (Ergebnis: e, Fassung: fassung, Abweichungen: abweichungen, Protokoll: Katalogabgleich.Protokoll().Count);
            });

            Assert.Equal(SchemaStand.Zielversion.ToString(CultureInfo.InvariantCulture),
                         Katalogupdate.Wert(db, "SELECT \"SchemaVersion\" FROM \"Tab_Applikation\""));
            Assert.False(befund.Ergebnis.Ausgefuehrt);
            Assert.Equal(0, sicherungen);
            Assert.Equal(Katalogupdate.FASSUNG_N, befund.Fassung);
            Assert.Empty(befund.Abweichungen);
            Assert.Equal(0, befund.Protokoll);
        }

        /// <summary>
        /// <b>Update über eine Installation mit älterer Fassung (Freigabeprobe 7 b) und danach mit gleicher Fassung
        /// (7 c).</b> In der Anwenderdatenbank der Fassung n hat der Anwender den gesperrten Satz B angepasst, eine
        /// eigene Zeile angelegt und ein Projekt mit einer Projektkopie geführt. Der Start mit dem Paket n + 1 legt
        /// die Sicherung an, führt den gesperrten Satz A nach, behält die Anpassung an B, lässt eigene Zeile und
        /// Projektdaten unverändert, schreibt das Protokoll und steht auf Fassung n + 1. Ein zweiter Start mit
        /// demselben Paket gleicht nicht ab und ändert nichts.
        /// </summary>
        [Fact]
        public void U2_Update_fuehrt_den_Paketsatz_nach_und_laesst_Anwender_und_Projektdaten_stehen()
        {
            if (!_k.Bereit) return;

            string ordner = _k.NeuerDatenordner();
            string db = Bereitstellen(ordner, _k.VorlageN);
            const string EIGENE = "Eigenes Modul (Probe Katalogpaket-Update)";
            Katalogupdate.Ausfuehren(db,
                "UPDATE \"Tab_BHKW_STAMM\" SET \"Pel\" = \"Pel\" + 7 WHERE \"Katalog_Schluessel\" = '" + _k.SchluesselB + "'",
                "INSERT INTO \"Tab_BHKW_STAMM\" (\"Bezeichner\", \"Pel\", \"Ptherm\", \"ReadOnly\", \"Katalog_Ausgelaufen\") " +
                "VALUES ('" + EIGENE + "', 50, 80, 0, 0)",
                "INSERT INTO \"Tab_Projekt\" (\"Projektname\") VALUES ('Probe Katalogpaket-Update')",
                "INSERT INTO \"Tab_BHKW\" (\"ID_Projekt\", \"Bezeichner\", \"Pel\", \"Ptherm\") SELECT (SELECT MAX(\"ID\") FROM " +
                "\"Tab_Projekt\"), \"Bezeichner\", \"Pel\", \"Ptherm\" FROM \"Tab_BHKW_STAMM\" WHERE \"Katalog_Schluessel\" = '" +
                _k.SchluesselA + "'");

            string Pel(string datei, string schluessel) => Katalogupdate.Wert(datei,
                "SELECT \"Pel\" FROM \"Tab_BHKW_STAMM\" WHERE \"Katalog_Schluessel\" = '" + schluessel + "'");
            string pelAPaket = Pel(_k.VorlageN1, _k.SchluesselA);
            string pelBAnwender = Pel(db, _k.SchluesselB);
            Assert.NotEqual(Pel(db, _k.SchluesselA), pelAPaket);
            List<string> eigeneVorher = Katalogupdate.Zeilen(db,
                "SELECT * FROM \"Tab_BHKW_STAMM\" WHERE \"Katalog_Schluessel\" IS NULL ORDER BY \"ID\"");
            List<string> projektVorher = Katalogupdate.Zeilen(db, "SELECT * FROM \"Tab_BHKW\" ORDER BY \"ID\"");
            projektVorher.AddRange(Katalogupdate.Zeilen(db, "SELECT * FROM \"Tab_Projekt\" ORDER BY \"ID\""));
            Assert.Contains(eigeneVorher, z => z.Contains(EIGENE));

            var befund = Gegen(db, () =>
            {
                KatalogabgleichErgebnis e = Katalogabgleich.BeimStart(Katalogpaket.Pfad(_k.VorlageN1), Katalogabgleich.SicherungAnlegen);
                return (Ergebnis: e, Fassung: Katalogabgleich.FassungDerDatenbank(), Protokoll: Katalogabgleich.Protokoll());
            });

            KatalogabgleichErgebnis erg = befund.Ergebnis;
            Assert.True(erg.Ausgefuehrt, erg.Meldung);
            Assert.Equal(Katalogupdate.FASSUNG_N, erg.FassungVorher);
            Assert.Equal(Katalogupdate.FASSUNG_N1, befund.Fassung);
            Assert.False(string.IsNullOrEmpty(erg.Sicherung));
            Assert.True(File.Exists(erg.Sicherung), "Die Sicherung fehlt: " + erg.Sicherung);
            Assert.Equal(Path.Combine(ordner, "DB-Backup"), Path.GetDirectoryName(erg.Sicherung));

            Assert.Contains(erg.Eintraege, x => x.Schluessel == _k.SchluesselA && x.Aktion == Katalogabgleich.AKTION_AKTUALISIERT);
            Assert.Contains(erg.Eintraege, x => x.Schluessel == _k.SchluesselB && x.Aktion == Katalogabgleich.AKTION_BEHALTEN);
            Assert.Equal(pelAPaket, Pel(db, _k.SchluesselA));
            Assert.Equal(pelBAnwender, Pel(db, _k.SchluesselB));
            Assert.Equal(eigeneVorher, Katalogupdate.Zeilen(db,
                "SELECT * FROM \"Tab_BHKW_STAMM\" WHERE \"Katalog_Schluessel\" IS NULL ORDER BY \"ID\""));
            List<string> projektNachher = Katalogupdate.Zeilen(db, "SELECT * FROM \"Tab_BHKW\" ORDER BY \"ID\"");
            projektNachher.AddRange(Katalogupdate.Zeilen(db, "SELECT * FROM \"Tab_Projekt\" ORDER BY \"ID\""));
            Assert.Equal(projektVorher, projektNachher);
            Assert.Contains(befund.Protokoll, p => p.Fassung == Katalogupdate.FASSUNG_N1 && p.Schluessel == _k.SchluesselA &&
                                                   p.Aktion == Katalogabgleich.AKTION_AKTUALISIERT);

            // 7 c: dieselbe Fassung noch einmal — kein Abgleich, keine Sicherung, kein Protokoll.
            string stand = Vorlage.Pruefsumme(db);
            int sicherungen = 0;
            var zweiter = Gegen(db, () => (Ergebnis: Katalogabgleich.BeimStart(Katalogpaket.Pfad(_k.VorlageN1),
                                                                                 () => { sicherungen++; return ""; }),
                                           Protokoll: Katalogabgleich.Protokoll().Count));
            Assert.False(zweiter.Ergebnis.Ausgefuehrt);
            Assert.Equal(0, sicherungen);
            Assert.Equal(befund.Protokoll.Count, zweiter.Protokoll);
            Assert.Equal(stand, Vorlage.Pruefsumme(db));
        }

        /// <summary>
        /// <b>Später gesperrter Satz (Konzept Setup 6.5.5):</b> Ein Satz, der im Modus <c>alle</c> ungesperrt
        /// ausgeliefert wurde, liegt beim Anwender ohne Schlüssel. Sperrt die Quelle ihn später, trägt das Paket n + 1
        /// ihn mit einem neu gebildeten Schlüssel. Der Abgleich bindet die eine gleichnamige, ungesperrte Zeile ohne
        /// Schlüssel an (Aktion <c>ANGEBUNDEN</c>, Inhalt gleich): Schlüssel, Prüfsumme des Pakets, gesperrt, dieselbe
        /// ID, kein Doppel. Die Fassung n + 2 mit neuer Leistung führt die angebundene, unveränderte Zeile nach.
        /// </summary>
        [Fact]
        public void U3_Spaeter_gesperrter_Satz_wird_angebunden_und_von_der_Folgefassung_nachgefuehrt()
        {
            if (!_k.Bereit) return;

            string db = Bereitstellen(_k.NeuerDatenordner(), _k.VorlageN);
            string name = _k.NameD.Replace("'", "''");
            string abfrage = "SELECT \"ID\", \"ReadOnly\", COALESCE(\"Katalog_Schluessel\", '-'), COALESCE(\"Katalog_Pruefsumme\", '-'), " +
                             "\"Ptherm\" FROM \"Tab_Heizkessel_STAMM\" WHERE \"Bezeichner\" = '" + name + "' ORDER BY \"ID\"";
            List<string> vorher = Katalogupdate.Zeilen(db, abfrage);
            Assert.Single(vorher);
            string[] v = vorher[0].Split('|');
            Assert.Equal(("0", "-", "-"), (v[1], v[2], v[3]));
            Katalogpaketsatz Satz(string paket) => Katalogpaket.Lesen(paket).Tabellen
                                                               .Single(t => t.Tabelle == "Tab_Heizkessel_STAMM").Saetze
                                                               .Single(x => x.Bezeichner == _k.NameD);
            Katalogpaketsatz n1 = Satz(_k.PaketN1);
            Katalogpaketsatz n2 = Satz(_k.PaketN2);
            Assert.Equal(n1.Schluessel, n2.Schluessel);
            Assert.NotEqual(n1.Pruefsumme, n2.Pruefsumme);

            KatalogabgleichErgebnis e = Gegen(db, () =>
                Katalogabgleich.BeimStart(Katalogpaket.Pfad(_k.VorlageN1), () => ""));

            Assert.True(e.Ausgefuehrt, e.Meldung);
            KatalogabgleichEintrag d = e.Eintraege.Single(x => x.Tabelle == "Tab_Heizkessel_STAMM");
            Assert.Equal((n1.Schluessel, Katalogabgleich.AKTION_ANGEBUNDEN), (d.Schluessel, d.Aktion));
            Assert.False(d.Wiederherstellbar);
            Assert.Equal(1, e.Angebunden);
            Assert.Equal(new[] { string.Join("|", v[0], "1", n1.Schluessel, n1.Pruefsumme, v[4]) },
                         Katalogupdate.Zeilen(db, abfrage));

            // Fassung n + 2: die angebundene Zeile ist unverändert - sie wird nachgeführt, an derselben ID.
            string pthermN2 = Katalogupdate.Wert(_k.VorlageN2, "SELECT \"Ptherm\" FROM \"Tab_Heizkessel_STAMM\" WHERE \"Bezeichner\" = '" +
                                                               name + "'");
            Assert.NotEqual(v[4], pthermN2);
            KatalogabgleichErgebnis f = Gegen(db, () =>
                Katalogabgleich.BeimStart(Katalogpaket.Pfad(_k.VorlageN2), () => ""));
            Assert.True(f.Ausgefuehrt, f.Meldung);
            Assert.Equal(Katalogupdate.FASSUNG_N1, f.FassungVorher);
            KatalogabgleichEintrag d2 = f.Eintraege.Single(x => x.Tabelle == "Tab_Heizkessel_STAMM");
            Assert.Equal((n2.Schluessel, Katalogabgleich.AKTION_AKTUALISIERT), (d2.Schluessel, d2.Aktion));
            Assert.Equal(new[] { string.Join("|", v[0], "1", n2.Schluessel, n2.Pruefsumme, pthermN2) },
                         Katalogupdate.Zeilen(db, abfrage));
        }
    }
}
