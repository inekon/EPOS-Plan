using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ETAPPE E16 — <b>die Wiederholperiode je Kostenposition</b> (Konzept Wirtschaftlichkeit
    /// § 2.11.2 V‑G3, DIN EN 17463 6.3.1 „alle n Jahre"; Schemaschritt
    /// <see cref="WiederholperiodeSchema.SCHRITT"/>).
    ///
    /// <para>Fünf Teile: (1) der Schemaschritt und die Werkzeug-Wache; (2) die Regel der
    /// Zahlungsjahre und der Rechenkern — n = 1 bitgleich, n = 2 zahlt in 1, 3, 5 …, mit
    /// Startjahr 3 in 3, 5 …, der Kapitalwert gegen die Handrechnung; (3) der Lese- und
    /// Schreibweg samt Vorlagenübernahme; (4) Lauf, Gliederungsprobe und Berichte; (5) die
    /// Formelmappe.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class WiederholperiodeTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static readonly CultureInfo DE = CultureInfo.GetCultureInfo("de-DE");

        private const int STAMM = 1040;
        private const int VARIANTE_A = 1041;     // ohne eigene Betriebszeilen in der Testdatenbank
        private const string ERWARTET = WirtschaftlichkeitSzenario.ERWARTET;

        // =====================================================================
        //  (1) Der Schemaschritt und die Werkzeug-Wache
        // =====================================================================

        /// <summary>Der Schritt legt eine nullbare INTEGER-Spalte an beiden Tabellen an und
        /// ist der Zielstand.</summary>
        [Fact]
        public void Der_Schritt_fuehrt_die_Spalte_an_beiden_Tabellen()
        {
            Assert.True(SchemaStand.Zielversion >= WiederholperiodeSchema.SCHRITT);
            Assert.True(WiederholperiodeSchema.SCHRITT > ProjektWirkungSchema.SCHRITT,
                        "Der Schritt muss nach 127 stehen.");
            Assert.Equal(2, WiederholperiodeSchema.Spalten.Length);
            Assert.Equal(new[] { SchemaKatalog.TAB_PROJEKTWERTE, SchemaKatalog.TAB_KOSTENVORLAGEPOSITION },
                         WiederholperiodeSchema.Spalten.Select(s => s.Tabelle).ToArray());
            foreach (SchemaSpalte s in WiederholperiodeSchema.Spalten)
            {
                Assert.Equal("Wiederholperiode_a", s.Name);
                Assert.Equal("INTEGER", StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition));
            }
            // Nicht in Alle: Leser ist allein die Kostenwelt.
            Assert.DoesNotContain(SchemaKatalog.Alle, s => s.Name == WiederholperiodeSchema.SPALTE);
        }

        /// <summary>
        /// DIE WERKZEUG-WACHE (Quelle): Migration, Werkzeug und Nachzieh-Liste der
        /// Testvorrichtung ziehen den Schritt aus DERSELBEN Quelle, die Nummer steht allein bei
        /// <see cref="WiederholperiodeSchema.SCHRITT"/>, und in der Schrittliste der Schale
        /// steht der Schritt nach 127.
        /// </summary>
        [Fact]
        public void Migration_Werkzeug_und_Testdatenbank_ziehen_den_Schritt_aus_einer_Quelle()
        {
            string wurzel = Wurzel();
            if (wurzel == null) return;

            string migration = File.ReadAllText(Path.Combine(wurzel, "WindowsFormsApplication1", "Allgemein", "Update", "SchemaMigration.cs"));
            Assert.Contains("public const int SCHRITT_WIEDERHOLPERIODE = WiederholperiodeSchema.SCHRITT;", migration, StringComparison.Ordinal);
            int ort127 = migration.IndexOf("new Schritt(SCHRITT_127_NICHT_MONETAERE_WIRKUNGEN", StringComparison.Ordinal);
            int ortE16 = migration.IndexOf("new Schritt(SCHRITT_WIEDERHOLPERIODE", StringComparison.Ordinal);
            Assert.True(ort127 > 0 && ortE16 > ort127, "Der Schritt steht nicht nach 127 in der Schrittliste.");
            Assert.Contains("private static bool Schritt_Wiederholperiode(Lauf l)", migration, StringComparison.Ordinal);
            Assert.Contains("WiederholperiodeSchema.Spalten", migration, StringComparison.Ordinal);

            string werkzeug = File.ReadAllText(Path.Combine(wurzel, "Werkzeuge", "Testdatenbankschema", "Program.cs"));
            Assert.Contains("WiederholperiodeSchema.Spalten", werkzeug, StringComparison.Ordinal);
            Assert.Contains("WiederholperiodeSchema.SCHRITT, trocken", werkzeug, StringComparison.Ordinal);

            string vorrichtung = File.ReadAllText(Path.Combine(wurzel, "EPOS.Kern.Tests", "TestDatenbank.cs"));
            Assert.Contains("WiederholperiodeSchema.Spalten", vorrichtung, StringComparison.Ordinal);

            string stand = File.ReadAllText(Path.Combine(wurzel, "EPOS.Kern", "Allgemein", "Update", "SchemaStand.cs"));
            Assert.Contains("public const int Zielversion = WiederholperiodeSchema.SCHRITT;", stand, StringComparison.Ordinal);
        }

        /// <summary>
        /// DIE WERKZEUG-WACHE (Datei): Die REPO-Datei der Testdatenbank steht auf dem Stand des
        /// Schritts oder später, trägt die Spalte an beiden Tabellen — und keine Zeile pflegt sie:
        /// Die Referenzprojekte zahlen jährlich, der Referenzlauf bleibt byte-gleich.
        /// Schreibgeschützt und ohne Spuren gelesen (<c>mode=ro&amp;immutable=1</c>).
        /// </summary>
        [Fact]
        public void Die_Repo_Testdatenbank_traegt_die_Spalte_leer_auf_dem_Stand_des_Schritts()
        {
            string wurzel = Wurzel();
            if (wurzel == null) return;
            string pfad = Path.Combine(wurzel, "Referenzlaeufe", "Kenndaten_Test.sqlite");
            if (!File.Exists(pfad)) return;
            LfsZeigerProbe.Sicherstellen(pfad);

            string uri = "file:" + pfad.Replace('\\', '/').Replace("?", "%3f") + "?mode=ro&immutable=1";
            using var c = new Microsoft.Data.Sqlite.SqliteConnection(
                new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder { DataSource = uri }.ToString());
            c.Open();

            long stand = Skalar(c, "SELECT SchemaVersion FROM Tab_Applikation LIMIT 1");
            Assert.True(stand >= WiederholperiodeSchema.SCHRITT,
                        "Die Testdatenbank steht auf Schemastand " + stand + ", erwartet mindestens " +
                        WiederholperiodeSchema.SCHRITT + ".");
            foreach (SchemaSpalte s in WiederholperiodeSchema.Spalten)
            {
                Assert.True(Skalar(c, "SELECT COUNT(*) FROM pragma_table_info($t) WHERE name = $s", s.Tabelle, s.Name) == 1,
                            "Der Testdatenbank fehlt " + s.Tabelle + "." + s.Name + " — Werkzeug Testdatenbankschema ziehen.");
                Assert.True(Skalar(c, "SELECT COUNT(*) FROM [" + s.Tabelle + "] WHERE [" + s.Name + "] IS NOT NULL") == 0,
                            "Die Testdatenbank pflegt eine Wiederholperiode in " + s.Tabelle + ".");
            }
        }

        // =====================================================================
        //  (2) Die Regel der Zahlungsjahre und der Rechenkern
        // =====================================================================

        /// <summary>E16‑Q1 a: Die Position zahlt ab ihrem Startjahr s in s, s + n, s + 2n …;
        /// ohne Startjahr ab Jahr 1, mit n ≤ 1 jährlich.</summary>
        [Theory]
        [InlineData(null, null, "1,2,3,4,5,6,7,8,9,10")]
        [InlineData(1, null, "1,2,3,4,5,6,7,8,9,10")]
        [InlineData(0, null, "1,2,3,4,5,6,7,8,9,10")]
        [InlineData(2, null, "1,3,5,7,9")]
        [InlineData(2, 1, "1,3,5,7,9")]
        [InlineData(2, 3, "3,5,7,9")]
        [InlineData(3, 2, "2,5,8")]
        [InlineData(4, 8, "8")]
        [InlineData(20, 1, "1")]
        public void Die_Zahlungsjahre_folgen_der_Regel(int? periode, int? start, string jahre)
        {
            Assert.Equal(jahre, string.Join(",", Wiederholperiode.Zahlungsjahre(periode, start, 10)));
        }

        /// <summary>Leer, 0, 1 und Negatives heißen jährlich; über der Grenze wird geklemmt.</summary>
        [Theory]
        [InlineData(null, null)]
        [InlineData(-3, null)]
        [InlineData(0, null)]
        [InlineData(1, null)]
        [InlineData(2, 2)]
        [InlineData(15, 15)]
        [InlineData(500, 99)]
        public void Die_Periode_wird_normiert(int? roh, int? erwartet)
        {
            Assert.Equal(erwartet, Wiederholperiode.Normiert(roh));
        }

        /// <summary>
        /// n = 1 BITGLEICH: Ohne Liste, mit leerer Liste und mit einer jährlichen Position (n = 1)
        /// über die Liste rechnet der Kern Zahl für Zahl dasselbe wie der Betriebstopf von vorher.
        /// </summary>
        [Fact]
        public void Jaehrlich_rechnet_der_Kern_bitgleich()
        {
            KapitalwertRechner.Zahlungsbild alt = Bild(2000, null);
            KapitalwertRechner.Zahlungsbild leer = Bild(2000, new List<KapitalwertRechner.Wiederholposten>());
            KapitalwertRechner.Zahlungsbild ueberListe = Bild(0, new List<KapitalwertRechner.Wiederholposten>
            {
                new KapitalwertRechner.Wiederholposten { Betrag = 2000, StartJahr = 1, Periode = 1 }
            });

            foreach (KapitalwertRechner.Zahlungsbild b in new[] { leer, ueberListe })
            {
                Assert.Equal(BitConverter.DoubleToInt64Bits(alt.Kapitalwert), BitConverter.DoubleToInt64Bits(b.Kapitalwert));
                for (int t = 0; t < alt.NominalReihe.Length; t++)
                {
                    Assert.Equal(alt.NominalReihe[t], b.NominalReihe[t]);
                    Assert.Equal(alt.BarwertReihe[t], b.BarwertReihe[t]);
                    Assert.Equal(alt.BetriebJeJahr[t], b.BetriebJeJahr[t]);
                }
            }
            Assert.Null(leer.Wiederholt);
        }

        /// <summary>n = 2 zahlt in Jahr 1, 3, 5 … — und in den übrigen Jahren nichts; der
        /// Kapitalwert trifft die Handrechnung Σ −B·(1+p_B)^(t−1)/(1+i)^t.</summary>
        [Fact]
        public void Alle_zwei_Jahre_zahlt_in_1_3_5_und_trifft_die_Handrechnung()
        {
            var w = new List<KapitalwertRechner.Wiederholposten>
            {
                new KapitalwertRechner.Wiederholposten { Betrag = 1000, StartJahr = 1, Periode = 2 }
            };
            KapitalwertRechner.Zahlungsbild b = KapitalwertRechner.Rechne(
                new List<KapitalwertRechner.InvestPosition>(), 0, 0, 0, 3.0, 10, 2.0, 1.0, wiederholt: w);

            double hand = 0;
            for (int t = 1; t <= 10; t++)
            {
                bool zahlt = t % 2 == 1;
                double erwartet = zahlt ? 1000 * Math.Pow(1.02, t - 1) : 0.0;
                Assert.Equal(erwartet, b.BetriebJeJahr[t], 9);
                Assert.Equal(zahlt ? 1000.0 : 0.0, b.BetriebBasisJeJahr[t], 12);
                if (zahlt) hand -= 1000 * Math.Pow(1.02, t - 1) / Math.Pow(1.03, t);
            }
            Assert.Equal(hand, b.Kapitalwert, 9);
            Assert.Equal(0.0, b.NominalReihe[0]);
            Assert.Same(w, b.Wiederholt);
        }

        /// <summary>Mit Startjahr 3 zahlt die Position in 3, 5, 7 …; der Endenergie-Topf
        /// schreibt mit p_E fort statt mit p_B.</summary>
        [Fact]
        public void Mit_Startjahr_3_zahlt_die_Position_in_3_5_7_und_folgt_ihrem_Topf()
        {
            var w = new List<KapitalwertRechner.Wiederholposten>
            {
                new KapitalwertRechner.Wiederholposten { Betrag = 500, StartJahr = 3, Periode = 2 },
                new KapitalwertRechner.Wiederholposten { Betrag = 300, StartJahr = 1, Periode = 3, Endenergie = true }
            };
            KapitalwertRechner.Zahlungsbild b = KapitalwertRechner.Rechne(
                new List<KapitalwertRechner.InvestPosition>(), 0, 0, 0, 3.0, 9, 2.0, 5.0, wiederholt: w);

            double hand = 0;
            for (int t = 1; t <= 9; t++)
            {
                double betrieb = (t >= 3 && (t - 3) % 2 == 0) ? 500 * Math.Pow(1.02, t - 1) : 0.0;
                double ende = ((t - 1) % 3 == 0) ? 300 * Math.Pow(1.05, t - 1) : 0.0;
                Assert.Equal(betrieb + ende, b.BetriebJeJahr[t], 9);
                Assert.Equal(ende, b.EndenergieAnteilJeJahr[t], 9);
                hand -= (betrieb + ende) / Math.Pow(1.03, t);
            }
            Assert.Equal(hand, b.Kapitalwert, 9);
        }

        /// <summary>Eine Position, deren Startjahr jenseits von T liegt, zahlt nie; eine mit
        /// n &gt; T zahlt genau einmal im Startjahr.</summary>
        [Fact]
        public void Am_Rand_des_Zeitraums_zahlt_die_Position_hoechstens_einmal()
        {
            var jenseits = new List<KapitalwertRechner.Wiederholposten>
            {
                new KapitalwertRechner.Wiederholposten { Betrag = 800, StartJahr = 12, Periode = 2 }
            };
            Assert.Equal(0.0, KapitalwertRechner.Rechne(new List<KapitalwertRechner.InvestPosition>(),
                0, 0, 0, 3.0, 10, 0, 0, wiederholt: jenseits).Kapitalwert);

            var einmal = new List<KapitalwertRechner.Wiederholposten>
            {
                new KapitalwertRechner.Wiederholposten { Betrag = 800, StartJahr = 4, Periode = 50 }
            };
            KapitalwertRechner.Zahlungsbild b = KapitalwertRechner.Rechne(new List<KapitalwertRechner.InvestPosition>(),
                0, 0, 0, 3.0, 10, 0, 0, wiederholt: einmal);
            Assert.Equal(-800 / Math.Pow(1.03, 4), b.Kapitalwert, 9);
            Assert.Equal(800.0, b.BetriebJeJahr.Sum(), 9);
        }

        // =====================================================================
        //  (3) Lese- und Schreibweg, Vorlagenübernahme, Nachweisumschlag
        // =====================================================================

        /// <summary>
        /// PERSISTENZ-RUNDLAUF: Die Periode wird geschrieben und gelesen; 1 und leer schreiben
        /// NULL; die Leseschleife legt die Zeile in die Liste „alle n Jahre" statt in die
        /// Jahres-Akkumulatoren.
        /// </summary>
        [Fact]
        public void Die_Periode_ueberlebt_Schreiben_und_Lesen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            PositionenAnlegen(null, null);
            int kessel = Zahl("SELECT ID FROM Tab_ProjektWerte WHERE ProjektID = 1041 AND Gruppe = 'Wartung Kessel'");

            Assert.True(WiederholperiodeSchema.SpalteVorhanden(SchemaKatalog.TAB_PROJEKTWERTE));
            Assert.True(Wiederholperiode.Schreibe(SchemaKatalog.TAB_PROJEKTWERTE, kessel, 2));
            Assert.Equal(2, Wiederholperiode.LiesProjekt(VARIANTE_A)[kessel]);

            WirtschaftlichkeitCtrl.BetriebsTopfe topfe = WirtschaftlichkeitCtrl.LiesBetriebskostenTopfe(VARIANTE_A, ERWARTET);
            Assert.Null(topfe.Fehler);
            Assert.Equal(1800.0, topfe.BetriebSofort, 9);
            KapitalwertRechner.Wiederholposten w = Assert.Single(topfe.Wiederholt);
            Assert.Equal(600.0, w.Betrag, 9);
            Assert.Equal(1, w.StartJahr);
            Assert.Equal(2, w.Periode);
            Assert.False(w.Endenergie);
            Assert.Equal(2400.0, topfe.Gesamt, 9);
            Assert.Equal(600.0, topfe.WiederholtErstesJahr, 9);

            KostenPositionNachweis n = WirtschaftlichkeitCtrl.LiesBetriebskostenPositionen(VARIANTE_A, ERWARTET)
                                                          .Single(x => x.Gruppe == "Wartung Kessel");
            Assert.Equal(2, n.Wiederholperiode);

            Assert.True(Wiederholperiode.Schreibe(SchemaKatalog.TAB_PROJEKTWERTE, kessel, 1));
            Assert.Equal(1, Zahl("SELECT COUNT(*) FROM Tab_ProjektWerte WHERE ID = " + kessel +
                                 " AND Wiederholperiode_a IS NULL"));
            Assert.Empty(Wiederholperiode.LiesProjekt(VARIANTE_A));
            Assert.Empty(WirtschaftlichkeitCtrl.LiesBetriebskostenTopfe(VARIANTE_A, ERWARTET).Wiederholt);

            Assert.False(Wiederholperiode.Schreibe("Tab_Projekt", kessel, 2));
            Assert.False(Wiederholperiode.Schreibe(SchemaKatalog.TAB_PROJEKTWERTE, 0, 2));
        }

        /// <summary>Die Vorlagenposition trägt die Periode, und die Vorlagenübernahme reicht sie
        /// in die frische Projektzeile.</summary>
        [Fact]
        public void Die_Vorlagenuebernahme_traegt_die_Periode_mit()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            object o = DataRepository.ExecuteScalar(
                "SELECT MIN(p.ID) FROM Tab_KostenVorlagePosition AS p INNER JOIN Tab_KostenVorlage AS v " +
                "ON p.VorlageID = v.ID WHERE v.KategorieID = 2");
            Assert.True(o != null && o != DBNull.Value, "Die Testdatenbank führt keine Betriebsvorlage.");
            int positionId = Convert.ToInt32(o);
            int vorlageId = Zahl("SELECT VorlageID FROM Tab_KostenVorlagePosition WHERE ID = " + positionId);

            Assert.True(Wiederholperiode.Schreibe(SchemaKatalog.TAB_KOSTENVORLAGEPOSITION, positionId, 3));
            KostenVorlagenPosition gelesen = KostenVorlagenCtrl.Positionen(vorlageId).Single(p => p.Id == positionId);
            Assert.Equal(3, gelesen.Wiederholperiode);
            KostenVorlagenPosition nachbar = KostenVorlagenCtrl.Positionen(vorlageId).FirstOrDefault(p => p.Id != positionId);
            if (nachbar != null) Assert.Null(nachbar.Wiederholperiode);

            KostenVorlageKopf kopf = KostenVorlagenCtrl.Vorlagen(
                    Zahl("SELECT KomponentenID FROM Tab_KostenVorlage WHERE ID = " + vorlageId), 2)
                .Single(k => k.Id == vorlageId);
            int neuesProjekt = Zahl("SELECT MAX(ID) FROM Tab_Projekt");
            int vorher = Zahl("SELECT COUNT(*) FROM Tab_ProjektWerte WHERE Wiederholperiode_a = 3");
            UebernahmeErgebnis e = KostenVorlagenUebernahmeCtrl.AusVorlage(neuesProjekt, kopf);
            Assert.True(e.Angelegt > 0 || e.Uebersprungen > 0, string.Join(" ", e.Meldungen));
            if (e.Angelegt > 0)
                Assert.True(Zahl("SELECT COUNT(*) FROM Tab_ProjektWerte WHERE Wiederholperiode_a = 3") > vorher);
        }

        /// <summary>Der Nachweisumschlag (Fassung 11) trägt die Periode hin und zurück und
        /// schreibt sie nur bei n ≥ 2; ein Umschlag der Fassung 10 liest sich „jährlich".</summary>
        [Fact]
        public void Der_Umschlag_traegt_die_Periode()
        {
            var e = new WirtschaftlichkeitErgebnis();
            e.Betriebskosten.Add(Fest("Wartung BHKW", 1800.0, null, null));
            e.Betriebskosten.Add(Fest("Dichtheitsprüfung", 400.0, 3, 2));

            string grund;
            string text = ErgebnisNachweisUmschlag.Schreiben(e, out grund);
            Assert.Null(grund);
            Assert.Equal(11, ErgebnisNachweisUmschlag.FASSUNG);
            Assert.Contains("\"Version\":" + ErgebnisNachweisUmschlag.FASSUNG, text);
            Assert.Single(text.Split(new[] { "\"Wiederholperiode\"" }, StringSplitOptions.None).Skip(1));
            Assert.Contains("\"Wiederholperiode\":2", text);

            var zurueck = new WirtschaftlichkeitErgebnis();
            ErgebnisNachweisUmschlag.Lesen(text).Uebernimm(zurueck);
            Assert.Null(zurueck.Betriebskosten[0].Wiederholperiode);
            Assert.Equal(2, zurueck.Betriebskosten[1].Wiederholperiode);

            var alt = new WirtschaftlichkeitErgebnis();
            ErgebnisNachweisUmschlag.Lesen(
                "nw1:{\"Version\":10,\"Betriebskosten\":[{\"Bezeichnung\":\"Wartung\",\"BetragJahr\":600}]}")
                .Uebernimm(alt);
            Assert.Null(Assert.Single(alt.Betriebskosten).Wiederholperiode);
        }

        // =====================================================================
        //  (4) Lauf, Gliederungsprobe und Berichte
        // =====================================================================

        /// <summary>Die Herleitungsspalte sagt „alle n Jahre ab Jahr X" — ohne Startjahr ab
        /// Jahr 1, hinter einer Bemessung mit Mittelpunkt, auf Englisch „every n years".</summary>
        [Fact]
        public void Die_Herleitungsspalte_nennt_die_Periode()
        {
            Assert.Equal("alle 2 Jahre ab Jahr 1", WirtschaftlichkeitZeilen.HerleitungZeile(Fest("K", 600, null, 2), DE));
            Assert.Equal("alle 2 Jahre ab Jahr 3", WirtschaftlichkeitZeilen.HerleitungZeile(Fest("K", 600, 3, 2), DE));
            Assert.Equal("ab Jahr 3", WirtschaftlichkeitZeilen.HerleitungZeile(Fest("K", 600, 3, null), DE));
            Assert.Equal("", WirtschaftlichkeitZeilen.HerleitungZeile(Fest("K", 600, null, null), DE));

            var szenario = Fest("S", 700.0, 4, 5);
            szenario.SzenarioGepflegt = true;
            Assert.Equal(R.WIRT_BK_SZENARIOWERT + " · alle 5 Jahre ab Jahr 4",
                         WirtschaftlichkeitZeilen.HerleitungZeile(szenario, DE));

            using (new Kulturvorrichtung("en-US"))
                Assert.Equal("every 2 years from year 3", WirtschaftlichkeitZeilen.HerleitungZeile(
                    Fest("K", 600, 3, 2), CultureInfo.GetCultureInfo("en-US")));
        }

        /// <summary>
        /// GLIEDERUNGSPROBE (E8c) und E16‑Q3 a: Eine Position „alle 2 Jahre" ab Jahr 1 zahlt im
        /// ersten Jahr — sie steht in den Betriebskosten p. a. (2.400 €), und die Probe geht auf;
        /// mit Startjahr 3 steht sie nicht darin (1.800 €), und die Probe geht ebenso auf. Beide
        /// Berichte nennen die Periode an der Position.
        /// </summary>
        [Fact]
        public void Die_Gliederung_geht_auf_und_die_Berichte_nennen_die_Periode()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            PositionenAnlegen(2, null);
            BerichtsDaten daten = Gruppe();
            Assert.Equal(2400.0, Ergebnis(daten).BetriebskostenJahr.Value, 9);

            string ordner = TempOrdner();
            try
            {
                using (XLWorkbook wb = Excel(daten, ordner))
                {
                    IXLWorksheet w = wb.Worksheet("Wirtschaftlichkeit");
                    Assert.Empty(Warnungen(w));
                    int titel = ZeileMitText(w, R.WIRT_BK_TITEL);
                    Assert.True(titel > 0, "Der Betriebskostenblock fehlt.");
                    IXLRow kessel = w.RowsUsed().First(z => z.RowNumber() > titel &&
                                                            z.Cell(2).GetString() == "Wartung Kessel");
                    Assert.Equal("alle 2 Jahre ab Jahr 1", kessel.Cell(4).GetString());
                    Assert.Equal(600.0, kessel.Cell(5).GetDouble(), 9);
                }
                string wort = Wort(daten, ordner);
                Assert.DoesNotContain(WarnungsAnfang(), wort);
                Assert.Contains("alle 2 Jahre ab Jahr 1", wort);
            }
            finally { Aufraeumen(ordner); }

            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_ProjektWerte SET StartJahr = 3 WHERE ProjektID = 1041 AND Gruppe = 'Wartung Kessel'");
            BerichtsDaten spaeter = Gruppe();
            WirtschaftlichkeitErgebnis e = Ergebnis(spaeter);
            Assert.Equal(1800.0, e.BetriebskostenJahr.Value, 9);
            KostenPositionNachweis n = e.Betriebskosten.Single(x => x.Gruppe == "Wartung Kessel");
            Assert.False(WirtschaftlichkeitZeilen.LaeuftImErstenJahr(n));
            Assert.Equal("alle 2 Jahre ab Jahr 3", WirtschaftlichkeitZeilen.HerleitungZeile(n, DE));
            double erstesJahr = e.Betriebskosten.Where(WirtschaftlichkeitZeilen.LaeuftImErstenJahr).Sum(x => x.BetragJahr);
            Assert.Equal("", WirtschaftlichkeitZeilen.GliederungAbweichung(erstesJahr, e.BetriebskostenJahr, DE));
        }

        /// <summary>
        /// KAPITALWERT GEGEN HANDRECHNUNG am echten Lauf: Die Wartung Kessel (600 €/a) auf
        /// „alle 2 Jahre" hebt den Kapitalwert der Variante genau um die Barwerte der entfallenen
        /// geraden Jahre, Σ 600·(1+p_B)^(t−1)/(1+i)^t, t = 2, 4 … ≤ T; die Zahlungsgliederung
        /// trägt in den geraden Jahren keine Kesselwartung mehr.
        /// </summary>
        [Fact]
        public void Der_Kapitalwert_des_Laufs_trifft_die_Handrechnung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            PositionenAnlegen(null, null);
            double vorher = Kw(Ergebnis(Gruppe()));

            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_ProjektWerte SET Wiederholperiode_a = 2 WHERE ProjektID = 1041 AND Gruppe = 'Wartung Kessel'");
            BerichtsDaten daten = Gruppe();
            double nachher = Kw(Ergebnis(daten));

            WirtschaftlichkeitParameter p = new WirtschaftlichkeitCtrl().LadeParameter(STAMM).FuerSzenario(ERWARTET);
            double i = p.Zinssatz / 100.0, pB = p.PreissteigerungBetrieb / 100.0;
            double hand = 0;
            for (int t = 2; t <= p.Betrachtungszeitraum; t += 2)
                hand += 600.0 * Math.Pow(1.0 + pB, t - 1) / Math.Pow(1.0 + i, t);
            Assert.True(hand > 0);
            // Der Kapitalwert steht auf Cent gerundet — die Differenz zweier Rundungen trifft die
            // Handrechnung auf einen Cent.
            Assert.True(Math.Abs(hand - (nachher - vorher)) <= 0.011,
                        "Handrechnung " + hand.ToString("R", CultureInfo.InvariantCulture) + ", Lauf " +
                        (nachher - vorher).ToString("R", CultureInfo.InvariantCulture));
        }

        // =====================================================================
        //  (5) Die Formelmappe
        // =====================================================================

        /// <summary>
        /// FORMELMAPPE (Stufe 1): Die Positionen „alle n Jahre" stehen je Topf in einer eigenen
        /// Hilfsspalte mit der Schutzformel IF(AND(Jahr&gt;=s,MOD(Jahr-s,n)=0),Betrag,0); die
        /// Betriebszelle rechnet (Basis + Wiederholt)·(1+p)^(Jahr−1); jede Formel besteht die
        /// Gegenrechnung (Wertfassung = Formelfassung), und ClosedXML rechnet die Hilfszellen
        /// zu denselben Zahlen.
        /// </summary>
        [Fact]
        public void Die_Formelmappe_rechnet_die_Periode_in_Formeln()
        {
            WirtschaftlichkeitParameter p = Satz();
            var w = new List<KapitalwertRechner.Wiederholposten>
            {
                new KapitalwertRechner.Wiederholposten { Betrag = 600, StartJahr = 1, Periode = 2 },
                new KapitalwertRechner.Wiederholposten { Betrag = 300, StartJahr = 2, Periode = 3, Endenergie = true }
            };
            KapitalwertRechner.Zahlungsbild bild = KapitalwertRechner.Rechne(
                new List<KapitalwertRechner.InvestPosition>
                {
                    new KapitalwertRechner.InvestPosition { Betrag = 100000, Nutzungsdauer = 25 }
                },
                2000, 10000, 15000, 3.0, 20, 1.0, 2.0, wiederholt: w);
            var serie = new VerlaufSerie { IdProjekt = VARIANTE_A, Bild = bild, RestwertBarwert = bild.RestwertBarwert };
            serie.Kumuliert = new double[bild.BarwertReihe.Length];
            double kum = 0;
            for (int t = 0; t < bild.BarwertReihe.Length; t++) { kum += bild.BarwertReihe[t]; serie.Kumuliert[t] = kum; }
            Mehrjahresbild m = Mehrjahresbild.Baue(serie);

            using var wb = new XLWorkbook();
            IXLWorksheet ws = wb.AddWorksheet("Wirtschaftlichkeit");
            var register = new Formelregister();
            int naechste = ExcelFormelmappe.Parameterblock(ws, 1, p, null, register);
            int kopf = naechste + 1;
            MehrjahresTafel tafel = ExcelFormelmappe.Mehrjahrestabelle(ws, kopf, VARIANTE_A, m, serie,
                                                                       p.FuerSzenario(ERWARTET), ERWARTET,
                                                                       m.Jahre, register);
            Assert.NotNull(tafel);
            Assert.Equal(0, register.Abweichungen);
            // Die Jahresspalte schreibt im Bericht der Tabellenbau; hier steht sie von Hand.
            for (int t = 0; t <= tafel.Jahre; t++) ws.Cell(tafel.Zeile(t), 1).Value = t;

            int hWB = SpalteMitKopf(ws, kopf, R.WIRT_FM_MJ_WDH_PB);
            int hWE = SpalteMitKopf(ws, kopf, R.WIRT_FM_MJ_WDH_PE);
            Assert.True(hWB > 0 && hWE > 0, "Die Hilfsspalten „alle n Jahre“ fehlen.");
            int cBetrieb = ExcelFormelmappe.SpalteVon(m, "BETRIEB");

            for (int t = 1; t <= 6; t++)
            {
                IXLCell zb = ws.Cell(tafel.Zeile(t), hWB);
                IXLCell ze = ws.Cell(tafel.Zeile(t), hWE);
                Assert.Contains("MOD(", zb.FormulaA1, StringComparison.Ordinal);
                Assert.Contains("MOD(", ze.FormulaA1, StringComparison.Ordinal);
                Assert.Equal(t % 2 == 1 ? 600.0 : 0.0, zb.GetDouble(), 9);                  // ClosedXML rechnet die Formel
                Assert.Equal(t >= 2 && (t - 2) % 3 == 0 ? 300.0 : 0.0, ze.GetDouble(), 9);
                Assert.Contains(ExcelFormelmappe.Bezug(tafel.Zeile(t), hWB), ws.Cell(tafel.Zeile(t), cBetrieb).FormulaA1,
                                StringComparison.Ordinal);
            }
        }

        /// <summary>Ohne Position „alle n Jahre" entsteht keine Hilfsspalte — die Mappe ist die
        /// von vorher.</summary>
        [Fact]
        public void Ohne_Periode_bleibt_die_Formelmappe_die_von_vorher()
        {
            WirtschaftlichkeitParameter p = Satz();
            KapitalwertRechner.Zahlungsbild bild = KapitalwertRechner.Rechne(
                new List<KapitalwertRechner.InvestPosition>(), 2000, 10000, 15000, 3.0, 20, 1.0, 2.0);
            var serie = new VerlaufSerie { IdProjekt = VARIANTE_A, Bild = bild, RestwertBarwert = bild.RestwertBarwert };
            serie.Kumuliert = new double[bild.BarwertReihe.Length];
            double kum = 0;
            for (int t = 0; t < bild.BarwertReihe.Length; t++) { kum += bild.BarwertReihe[t]; serie.Kumuliert[t] = kum; }
            Mehrjahresbild m = Mehrjahresbild.Baue(serie);

            using var wb = new XLWorkbook();
            IXLWorksheet ws = wb.AddWorksheet("Wirtschaftlichkeit");
            var register = new Formelregister();
            int kopf = ExcelFormelmappe.Parameterblock(ws, 1, p, null, register) + 1;
            ExcelFormelmappe.Mehrjahrestabelle(ws, kopf, VARIANTE_A, m, serie, p.FuerSzenario(ERWARTET), ERWARTET,
                                               m.Jahre, register);
            Assert.Equal(0, SpalteMitKopf(ws, kopf, R.WIRT_FM_MJ_WDH_PB));
            Assert.Equal(0, SpalteMitKopf(ws, kopf, R.WIRT_FM_MJ_WDH_PE));
            Assert.Equal(0, register.Abweichungen);
        }

        // =====================================================================
        //  Helfer
        // =====================================================================

        private static KapitalwertRechner.Zahlungsbild Bild(double betrieb, List<KapitalwertRechner.Wiederholposten> w)
        {
            var invest = new List<KapitalwertRechner.InvestPosition>
            {
                new KapitalwertRechner.InvestPosition { Betrag = 100000, Nutzungsdauer = 15 }
            };
            return w == null
                ? KapitalwertRechner.Rechne(invest, betrieb, 10000, 15000, 3.0, 20, 1.0, 2.0)
                : KapitalwertRechner.Rechne(invest, betrieb, 10000, 15000, 3.0, 20, 1.0, 2.0, wiederholt: w);
        }

        private static KostenPositionNachweis Fest(string gruppe, double betrag, int? start, int? periode)
        {
            return new KostenPositionNachweis
            {
                Bezeichnung = gruppe, Gruppe = gruppe, Kostenart = DbWerte.KOSTENART_BETRIEBSGEBUNDEN,
                Bemessung = DbWerte.BEMESSUNG_BETRAG, BetragJahr = betrag, StartJahr = start,
                Wiederholperiode = periode
            };
        }

        /// <summary>Die zwei Betriebspositionen des Prüffalls (Muster „hybtest"): Wartung BHKW
        /// 1.800 €/a jährlich, Wartung Kessel 600 € — mit Periode und Startjahr nach Wahl.</summary>
        private static void PositionenAnlegen(int? periode, int? start)
        {
            DataRepository.ExecuteNonQuery(
                "INSERT INTO Tab_ProjektWerte (ProjektID, StammID, KomponentenID, KategorieID, EingegebenerWert, " +
                "Gruppe, Kostenart, Bemessung) VALUES (1041, 83, 7, 2, 1800.0, 'Wartung BHKW', 'BETRIEBSGEBUNDEN', 'BETRAG')");
            DataRepository.ExecuteNonQuery(
                "INSERT INTO Tab_ProjektWerte (ProjektID, StammID, KomponentenID, KategorieID, EingegebenerWert, " +
                "Gruppe, Kostenart, Bemessung, StartJahr, Wiederholperiode_a) VALUES (1041, 79, 2, 2, 600.0, " +
                "'Wartung Kessel', 'BETRIEBSGEBUNDEN', 'BETRAG', ?, ?)",
                Ganz("@s", start), Ganz("@n", periode));
        }

        private static DbParam Ganz(string name, int? wert)
        {
            var p = new DbParam(name, DbParamTyp.Integer);
            p.Wert = wert.HasValue ? (object)wert.Value : DBNull.Value;
            return p;
        }

        /// <summary>Stamm 1040 mit den Varianten 1041/1042 und synthetischen Energiekosten —
        /// gerechnet ohne zu speichern (Muster <c>BetriebskostenStartjahrGliederungTests</c>).</summary>
        private static BerichtsDaten Gruppe()
        {
            int[] ids = { STAMM, VARIANTE_A, 1042 };
            double[] energie = { 12000.0, 9000.0, 7000.0 };
            string[] namen = { "Stammprojekt", "Variante A", "Variante B" };
            var daten = new BerichtsDaten { IdStamm = ids[0], Stammprojektname = "Stammprojekt" };
            for (int i = 0; i < ids.Length; i++)
                daten.Varianten.Add(new VariantenDaten
                {
                    IdProjekt = ids[i], IstStamm = i == 0, Projektname = "Stammprojekt",
                    Variantenname = i == 0 ? "" : namen[i], Ergebnis = new ErgebnisModel(),
                    Energiekosten = energie[i]
                });
            WirtschaftlichkeitParameter p = new WirtschaftlichkeitCtrl().LadeParameter(ids[0]);
            p.IdStamm = ids[0];
            daten.Wirtschaftlichkeit = new WirtschaftlichkeitCtrl().Berechne(daten, p, 0, false);
            return daten;
        }

        private static WirtschaftlichkeitErgebnis Ergebnis(BerichtsDaten daten)
        {
            return daten.Wirtschaftlichkeit.Single(x => x.IdProjekt == VARIANTE_A && x.Szenario == ERWARTET);
        }

        private static double Kw(WirtschaftlichkeitErgebnis e)
        {
            Assert.True(e.Kapitalwert.HasValue, "Kein Kapitalwert: " + (e.Fehlgrund ?? e.Hinweis));
            return e.Kapitalwert.Value;
        }

        /// <summary>i = 3 %, T = 20 a, p_E 2 %/a, p_B 1 %/a (Muster E15).</summary>
        private static WirtschaftlichkeitParameter Satz()
        {
            return new WirtschaftlichkeitParameter
            {
                IdStamm = STAMM,
                Zinssatz = 3.0,
                Betrachtungszeitraum = 20,
                PreissteigerungEnergie = 2.0,
                PreissteigerungBetrieb = 1.0,
                Einspeiseverguetung = 0.08
            };
        }

        private static int SpalteMitKopf(IXLWorksheet ws, int kopf, string text)
        {
            IXLCell c = ws.Row(kopf).CellsUsed().FirstOrDefault(
                x => string.Equals(x.GetString(), text, StringComparison.Ordinal));
            return c == null ? 0 : c.Address.ColumnNumber;
        }

        private static BerichtsKonfiguration VolleKonfiguration()
        {
            var k = new BerichtsKonfiguration();
            foreach (BerichtsKonfiguration.BausteinDef d in BerichtsKonfiguration.AlleBausteine)
                k.AktiveBausteine.Add(d.Schluessel);
            return k;
        }

        private static XLWorkbook Excel(BerichtsDaten daten, string ordner)
        {
            string ziel = Path.Combine(ordner, "wiederholperiode.xlsx");
            new ExcelBerichtGenerator().Erzeuge(daten, VolleKonfiguration(), ziel);
            return new XLWorkbook(ziel);
        }

        private static string Wort(BerichtsDaten daten, string ordner)
        {
            string ziel = Path.Combine(ordner, "wiederholperiode.docx");
            new WordBerichtGenerator().Erzeuge(daten, VolleKonfiguration(), ziel);
            using WordprocessingDocument doc = WordprocessingDocument.Open(ziel, false);
            return string.Join("\n", doc.MainDocumentPart.Document.Body
                .Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>().Select(p => p.InnerText));
        }

        private static string WarnungsAnfang()
        {
            string t = R.WIRT_BK_ABWEICHUNG;
            return t.Substring(0, t.IndexOf("{0}", StringComparison.Ordinal));
        }

        private static int ZeileMitText(IXLWorksheet w, string text)
        {
            IXLCell c = w.Column(1).CellsUsed().FirstOrDefault(
                x => string.Equals(x.GetString().Trim(), text, StringComparison.Ordinal));
            return c == null ? 0 : c.Address.RowNumber;
        }

        private static List<string> Warnungen(IXLWorksheet w)
        {
            string anfang = WarnungsAnfang();
            return w.Column(1).CellsUsed().Select(c => c.GetString())
                    .Where(s => s.StartsWith(anfang, StringComparison.Ordinal)).ToList();
        }

        private static string TempOrdner()
        {
            string o = Path.Combine(Path.GetTempPath(), "epos-e16-wdh-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(o);
            return o;
        }

        private static void Aufraeumen(string ordner)
        {
            try { Directory.Delete(ordner, true); } catch { /* Aufräumen darf nicht scheitern */ }
        }

        private static int Zahl(string sql)
        {
            object o = DataRepository.ExecuteScalar(sql);
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o, CultureInfo.InvariantCulture);
        }

        /// <summary>Die Repowurzel über den Pfad DIESER Quelldatei, sonst aufwärts bis
        /// <c>WP-Plan.sln</c>; <c>null</c> ohne Fund.</summary>
        private static string Wurzel([System.Runtime.CompilerServices.CallerFilePath] string eigeneDatei = null)
        {
            if (!string.IsNullOrEmpty(eigeneDatei))
            {
                string kandidat = Path.GetDirectoryName(Path.GetDirectoryName(eigeneDatei));
                if (kandidat != null && File.Exists(Path.Combine(kandidat, "WP-Plan.sln"))) return kandidat;
            }
            DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d != null)
            {
                if (File.Exists(Path.Combine(d.FullName, "WP-Plan.sln"))) return d.FullName;
                d = d.Parent;
            }
            return null;
        }

        private static long Skalar(Microsoft.Data.Sqlite.SqliteConnection c, string sql, params string[] werte)
        {
            using Microsoft.Data.Sqlite.SqliteCommand b = c.CreateCommand();
            b.CommandText = sql;
            if (werte.Length > 0) b.Parameters.AddWithValue("$t", werte[0]);
            if (werte.Length > 1) b.Parameters.AddWithValue("$s", werte[1]);
            object o = b.ExecuteScalar();
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt64(o, CultureInfo.InvariantCulture);
        }
    }
}
