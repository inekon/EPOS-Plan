using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ETAPPE E15 — das <b>Risikomodul</b> (V‑G7, Konzept Wirtschaftlichkeit § 2.11.2; DIN EN
    /// 17463 Abschnitt 6.5 und Anhang F): das Risiko als Zuschlag auf den Kalkulationszins
    /// ODER als Zahlungsstromabzug R_loss × p_loss je Periode t ≥ 1, Vorgabe aus.
    ///
    /// <para><b>Was die Fälle festhalten:</b> die Nullregel (aus = bitgleich), die EINE
    /// Stelle der Regeln (<see cref="RisikoModul"/>, <see cref="WirtschaftlichkeitParameter.FuerSzenario"/>,
    /// <see cref="KapitalwertRechner.Rechne"/>), die Wirkung des Zuschlags wie i + Δ in allen
    /// drei Szenarien, den Abzug nur t ≥ 1 und nicht auf den Restwert, wen er trifft (jeden
    /// Stand außer der Referenz; eine Gruppe mit einem Stand trägt ihn selbst), den
    /// Speicherweg, den Ausweis nur bei Pflege, die Formelmappe (Stufe 0 und 1) und die
    /// Werkzeug-Wache der Testdatenbank (Schemaschritt 125).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class RisikoModulTests
    {
        private const int STAMM = 9101;          // synthetischer Stand ohne Zeile in Tab_Projekt
        private const int VARIANTE = 9102;
        private const int PROJEKT_OHNE_ZEILE = 1040;   // keine Parameterzeile: INSERT-Weg
        private const int PROJEKT_MIT_ZEILE = 1030;    // Parameterzeile vorhanden: UPDATE-Weg

        private const string ERWARTET = WirtschaftlichkeitSzenario.ERWARTET;
        private const string BEST = WirtschaftlichkeitSzenario.BEST;
        private const string WORST = WirtschaftlichkeitSzenario.WORST;

        private static readonly CultureInfo DE = new CultureInfo("de-DE");

        // =====================================================================
        // Schema (Schritt 125) und Werkzeug-Wache
        // =====================================================================

        /// <summary>Der Schritt legt vier nullbare Spalten an der Parametertabelle an.</summary>
        [Fact]
        public void Schritt125_fuehrt_vier_Spalten_an_der_Parametertabelle()
        {
            Assert.True(SchemaStand.Zielversion >= 125, "Zielstand " + SchemaStand.Zielversion + " liegt unter 125.");
            Assert.Equal(4, SchemaKatalog.RisikomodulSpalten.Length);
            foreach (SchemaSpalte s in SchemaKatalog.RisikomodulSpalten)
                Assert.Equal(SchemaKatalog.TAB_PROJEKTWIRTSCHAFT, s.Tabelle);

            var typ = SchemaKatalog.RisikomodulSpalten.ToDictionary(s => s.Name, s => s.TypDefinition);
            Assert.Equal("TEXT(10)", typ[SchemaKatalog.SPALTE_PW_RISIKO_ART]);
            Assert.Equal("DOUBLE", typ[SchemaKatalog.SPALTE_PW_RISIKO_ZINSZUSCHLAG]);
            Assert.Equal("DOUBLE", typ[SchemaKatalog.SPALTE_PW_RISIKO_VERLUST]);
            Assert.Equal("DOUBLE", typ[SchemaKatalog.SPALTE_PW_RISIKO_WAHRSCHEINLICHKEIT]);
            // Nicht in Alle: kein Rechenweg der Simulation liest sie.
            Assert.DoesNotContain(SchemaKatalog.Alle, s => s.Name == SchemaKatalog.SPALTE_PW_RISIKO_ART);
        }

        /// <summary>
        /// Migration, Werkzeug und Nachzieh-Liste der Testdatenbank ziehen den Schritt aus
        /// DERSELBEN Quelle; in der Schrittliste der Schale steht er nach 124.
        /// </summary>
        [Fact]
        public void Migration_Werkzeug_und_Testdatenbank_ziehen_den_Schritt_aus_einer_Quelle()
        {
            string wurzel = Wurzel();
            if (wurzel == null) return;

            string migration = File.ReadAllText(Path.Combine(wurzel, "WindowsFormsApplication1", "Allgemein", "Update", "SchemaMigration.cs"));
            Assert.Contains("public const int SCHRITT_125_RISIKOMODUL = 125;", migration, StringComparison.Ordinal);
            int ort124 = migration.IndexOf("new Schritt(SCHRITT_124_ZAPFPROFIL_LAUFANGABEN", StringComparison.Ordinal);
            int ort125 = migration.IndexOf("new Schritt(SCHRITT_125_RISIKOMODUL", StringComparison.Ordinal);
            Assert.True(ort124 > 0 && ort125 > ort124, "Schritt 125 steht nicht nach 124 in der Schrittliste.");
            Assert.Contains("private static bool Schritt_Risikomodul(Lauf l)", migration, StringComparison.Ordinal);
            Assert.Contains("SchemaKatalog.RisikomodulSpalten", migration, StringComparison.Ordinal);

            string werkzeug = File.ReadAllText(Path.Combine(wurzel, "Werkzeuge", "Testdatenbankschema", "Program.cs"));
            Assert.Contains("SchemaKatalog.RisikomodulSpalten", werkzeug, StringComparison.Ordinal);
            Assert.Contains("s.TypDefinition), 125, trocken", werkzeug, StringComparison.Ordinal);

            string vorrichtung = File.ReadAllText(Path.Combine(wurzel, "EPOS.Kern.Tests", "TestDatenbank.cs"));
            Assert.Contains("SchemaKatalog.RisikomodulSpalten", vorrichtung, StringComparison.Ordinal);
        }

        /// <summary>
        /// DIE WERKZEUG-WACHE: Die REPO-Datei der Testdatenbank steht auf Schemastand 125 oder
        /// später, trägt die vier Spalten — und keine ist gepflegt: Die Referenzprojekte rechnen
        /// ohne Risiko, der Referenzlauf bleibt byte-gleich. Schreibgeschützt und ohne Spuren
        /// gelesen (<c>mode=ro&amp;immutable=1</c>).
        /// </summary>
        [Fact]
        public void Die_Repo_Testdatenbank_traegt_die_vier_Spalten_leer_auf_Stand_125()
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
            Assert.True(stand >= 125, "Die Testdatenbank steht auf Schemastand " + stand + ", erwartet mindestens 125.");
            foreach (SchemaSpalte s in SchemaKatalog.RisikomodulSpalten)
            {
                Assert.True(Skalar(c, "SELECT COUNT(*) FROM pragma_table_info($t) WHERE name = $s", s.Tabelle, s.Name) == 1,
                            "Der Testdatenbank fehlt " + s.Tabelle + "." + s.Name + " — Werkzeug Testdatenbankschema ziehen.");
                Assert.True(Skalar(c, "SELECT COUNT(*) FROM [" + s.Tabelle + "] WHERE [" + s.Name + "] IS NOT NULL") == 0,
                            "Die Testdatenbank trägt ein Risiko in " + s.Name + " (die Referenzprojekte rechnen ohne).");
            }
        }

        // =====================================================================
        // Normierung und die Regeln des Moduls
        // =====================================================================

        [Theory]
        [InlineData(null, null)]
        [InlineData("", null)]
        [InlineData("  ", null)]
        [InlineData("zins", Risikoart.ZINS)]
        [InlineData(" ZINS ", Risikoart.ZINS)]
        [InlineData("Abzug", Risikoart.ABZUG)]
        [InlineData("BETA", null)]
        public void Die_Art_wird_normiert_und_Unbekanntes_heisst_aus(string roh, string erwartet)
        {
            Assert.Equal(erwartet, Risikoart.Normiert(roh));
        }

        /// <summary>Vorgabe aus: ohne Art, mit Zuschlag 0 oder ohne R_loss bzw. p_loss rechnet
        /// nichts; der Abzug ist R_loss × p_loss / 100, p_loss auf 100 % begrenzt.</summary>
        [Fact]
        public void Das_Modul_rechnet_nur_mit_vollstaendiger_Pflege()
        {
            var p = new WirtschaftlichkeitParameter();
            Assert.False(RisikoModul.Gepflegt(p));
            Assert.Equal(0.0, RisikoModul.Zinszuschlag(p));
            Assert.Equal(0.0, RisikoModul.AbzugJeJahr(p));
            Assert.Equal("", RisikoModul.Nachweis(p, DE));

            // Zahlen ohne Art rechnen nicht.
            p.RisikoZinszuschlag = 1.0; p.RisikoVerlust = 10000; p.RisikoWahrscheinlichkeit = 10;
            Assert.False(RisikoModul.Gepflegt(p));

            p.RisikoArt = Risikoart.ZINS;
            Assert.True(RisikoModul.ZinsAktiv(p));
            Assert.False(RisikoModul.AbzugAktiv(p));
            Assert.Equal(1.0, RisikoModul.Zinszuschlag(p));
            Assert.Equal(0.0, RisikoModul.AbzugJeJahr(p));
            p.RisikoZinszuschlag = 0.0;
            Assert.False(RisikoModul.Gepflegt(p));

            p.RisikoArt = Risikoart.ABZUG;
            Assert.True(RisikoModul.AbzugAktiv(p));
            Assert.Equal(1000.0, RisikoModul.AbzugJeJahr(p));
            p.RisikoWahrscheinlichkeit = null;
            Assert.False(RisikoModul.Gepflegt(p));
            p.RisikoWahrscheinlichkeit = 150;
            Assert.Equal(10000.0, RisikoModul.AbzugJeJahr(p));   // p_loss höchstens 100 %
            p.RisikoVerlust = -5;
            Assert.False(RisikoModul.AbzugAktiv(p));
        }

        /// <summary>Den Abzug trägt jeder Stand außer der Referenz; eine Gruppe mit einem Stand
        /// trägt ihn selbst.</summary>
        [Fact]
        public void Den_Abzug_traegt_jeder_Stand_ausser_der_Referenz()
        {
            var p = new WirtschaftlichkeitParameter
            { RisikoArt = Risikoart.ABZUG, RisikoVerlust = 10000, RisikoWahrscheinlichkeit = 10 };
            Assert.Equal(1000.0, RisikoModul.AbzugFuerStand(p, false, 3));
            Assert.Equal(0.0, RisikoModul.AbzugFuerStand(p, true, 3));
            Assert.Equal(1000.0, RisikoModul.AbzugFuerStand(p, true, 1));
            Assert.Equal(0.0, RisikoModul.AbzugFuerStand(new WirtschaftlichkeitParameter(), false, 3));
        }

        // =====================================================================
        // Der Zinszuschlag: FuerSzenario, die eine Stelle
        // =====================================================================

        /// <summary>Aus: Erwartet bleibt DIESELBE Referenz — der Regellauf ist bitgleich.</summary>
        [Fact]
        public void Aus_bleibt_Erwartet_dieselbe_Referenz()
        {
            var p = new WirtschaftlichkeitParameter { Zinssatz = 3.0 };
            Assert.Same(p, p.FuerSzenario(ERWARTET));
            p.RisikoArt = Risikoart.ABZUG; p.RisikoVerlust = 10000; p.RisikoWahrscheinlichkeit = 10;
            Assert.Same(p, p.FuerSzenario(ERWARTET));   // der Abzug wirkt nicht über den Zins
        }

        /// <summary>
        /// Zinszuschlag: jedes Szenario rechnet mit seinem Zins plus Zuschlag (E15‑Q1 a) —
        /// Erwartet als Kopie; ein zweiter Aufruf auf der Kopie schlägt nicht noch einmal auf,
        /// und der gespeicherte Satz bleibt unberührt.
        /// </summary>
        [Fact]
        public void Der_Zuschlag_gilt_in_allen_drei_Szenarien_genau_einmal()
        {
            var p = new WirtschaftlichkeitParameter
            { Zinssatz = 3.0, RisikoArt = Risikoart.ZINS, RisikoZinszuschlag = 1.0 };
            p.SatzWorst.Zinssatz = 6.0;                               // gepflegt, absolut

            WirtschaftlichkeitParameter e = p.FuerSzenario(ERWARTET);
            Assert.NotSame(p, e);
            Assert.Equal(4.0, e.Zinssatz);
            Assert.Equal(3.0, p.Zinssatz);
            Assert.Equal(3.0, p.FuerSzenario(BEST).Zinssatz);         // Vorgabe 2 % + 1
            Assert.Equal(7.0, p.FuerSzenario(WORST).Zinssatz);        // gepflegt 6 % + 1
            Assert.Equal(4.0, e.FuerSzenario(ERWARTET).Zinssatz);     // nicht doppelt
        }

        // =====================================================================
        // Der Zahlungsstromabzug im KapitalwertRechner
        // =====================================================================

        /// <summary>Ohne Abzug ist das Bild Zahl für Zahl das ohne den Parameter.</summary>
        [Fact]
        public void Ohne_Abzug_rechnet_der_Kern_bitgleich()
        {
            KapitalwertRechner.Zahlungsbild ohne = Bild(0.0, ohneParameter: true);
            KapitalwertRechner.Zahlungsbild null0 = Bild(0.0);

            Assert.Equal(ohne.Kapitalwert, null0.Kapitalwert);
            Assert.Equal(ohne.NominalReihe, null0.NominalReihe);
            Assert.Equal(ohne.BarwertReihe, null0.BarwertReihe);
            Assert.Null(null0.RisikoJeJahr);
            Assert.Equal(0.0, null0.BarwertRisiko);
        }

        /// <summary>
        /// Der Abzug mindert die Nettozahlung je Periode t ≥ 1 um R_loss × p_loss — nicht das
        /// Jahr 0, nicht den Restwert, nicht die Ausgaben- und Einnahmensummen; der
        /// Kapitalwert sinkt um den Barwert der Abzüge.
        /// </summary>
        [Fact]
        public void Der_Abzug_wirkt_nur_ab_Jahr_1_und_nicht_auf_den_Restwert()
        {
            const double d = 1000.0;
            KapitalwertRechner.Zahlungsbild ohne = Bild(0.0);
            KapitalwertRechner.Zahlungsbild mit = Bild(d);
            int T = ohne.NominalReihe.Length - 1;

            Assert.Equal(ohne.NominalReihe[0], mit.NominalReihe[0]);
            Assert.Equal(ohne.BarwertReihe[0], mit.BarwertReihe[0]);
            Assert.Equal(0.0, mit.RisikoJeJahr[0]);
            double barwert = 0;
            for (int t = 1; t <= T; t++)
            {
                Assert.Equal(ohne.NominalReihe[t] - d, mit.NominalReihe[t], 6);
                Assert.Equal(d, mit.RisikoJeJahr[t]);
                barwert += d / Math.Pow(1.03, t);
            }
            Assert.Equal(ohne.RestwertNominal, mit.RestwertNominal);
            Assert.Equal(ohne.RestwertBarwert, mit.RestwertBarwert);
            Assert.Equal(ohne.BarwertAusgaben, mit.BarwertAusgaben);
            Assert.Equal(ohne.BarwertEinnahmen, mit.BarwertEinnahmen);
            Assert.Equal(barwert, mit.BarwertRisiko, 6);
            Assert.Equal(ohne.Kapitalwert - barwert, mit.Kapitalwert, 6);

            // Der interne Zinsfuß liest die risikobereinigte Differenzreihe.
            double[] reihe = KapitalwertRechner.Differenzreihe(mit, ohne);
            Assert.Equal(0.0, reihe[0]);
            for (int t = 1; t <= T; t++) Assert.Equal(-d, reihe[t], 6);
        }

        // =====================================================================
        // Der Lauf (WirtschaftlichkeitCtrl.Berechne, Verlauf)
        // =====================================================================

        /// <summary>
        /// ZINSZUSCHLAG wie i + Δ: Die Gruppe mit Zuschlag 1 %-Punkt rechnet in ALLEN drei
        /// Szenarien Zahl für Zahl wie dieselbe Gruppe mit einem um 1 %-Punkt höheren
        /// Kalkulationszins — Kapitalwerte, Differenz, Annuität, Amortisation, Zinsfuß.
        /// </summary>
        [Fact]
        public void Der_Zinszuschlag_verschiebt_den_Kapitalwert_wie_i_plus_Delta()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            WirtschaftlichkeitParameter mitZuschlag = Satz();
            mitZuschlag.RisikoArt = Risikoart.ZINS;
            mitZuschlag.RisikoZinszuschlag = 1.0;
            WirtschaftlichkeitParameter hoeher = Satz();
            hoeher.Zinssatz = 4.0;

            List<WirtschaftlichkeitErgebnis> a = Lauf(Gruppe(2), mitZuschlag);
            List<WirtschaftlichkeitErgebnis> b = Lauf(Gruppe(2), hoeher);
            Assert.Equal(6, a.Count);
            for (int i = 0; i < a.Count; i++)
            {
                Assert.Equal(b[i].Szenario, a[i].Szenario);
                Assert.Equal(Kw(b[i]), Kw(a[i]));
                Assert.Equal(b[i].KapitalwertDiff, a[i].KapitalwertDiff);
                Assert.Equal(b[i].AnnuitaetKW, a[i].AnnuitaetKW);
                Assert.Equal(b[i].AmortisationJahre, a[i].AmortisationJahre);
                Assert.Equal(b[i].IRR, a[i].IRR);
            }

            // Und ohne Zuschlag weicht er ab — der Zuschlag wirkt.
            List<WirtschaftlichkeitErgebnis> ohne = Lauf(Gruppe(2), Satz());
            Assert.NotEqual(Kw(ohne[0]), Kw(a[0]));
        }

        /// <summary>
        /// ZAHLUNGSSTROMABZUG in der Gruppe: Der Stamm (Referenz) bleibt bitgleich, die
        /// Variante verliert den Barwert von 1.000 € je Jahr — in allen drei Szenarien, mit
        /// dem Zins des Szenarios; die Kapitalwertdifferenz folgt.
        /// </summary>
        [Fact]
        public void Der_Abzug_trifft_die_Variante_und_nicht_die_Referenz()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            WirtschaftlichkeitParameter aus = Satz();
            WirtschaftlichkeitParameter abzug = Satz();
            abzug.RisikoArt = Risikoart.ABZUG;
            abzug.RisikoVerlust = 10000;
            abzug.RisikoWahrscheinlichkeit = 10;

            List<WirtschaftlichkeitErgebnis> vorher = Lauf(Gruppe(2), aus);
            List<WirtschaftlichkeitErgebnis> nachher = Lauf(Gruppe(2), abzug);
            for (int i = 0; i < vorher.Count; i++)
            {
                WirtschaftlichkeitErgebnis v = vorher[i], n = nachher[i];
                if (v.IdProjekt == STAMM)
                {
                    Assert.Equal(Kw(v), Kw(n));
                    continue;
                }
                double zins = aus.FuerSzenario(v.Szenario).Zinssatz / 100.0;
                double barwert = 0;
                for (int t = 1; t <= 20; t++) barwert += 1000.0 / Math.Pow(1.0 + zins, t);
                Assert.Equal(Kw(v) - barwert, Kw(n), 4);
                Assert.Equal(v.KapitalwertDiff.Value - barwert, n.KapitalwertDiff.Value, 4);
            }
        }

        /// <summary>Eine Gruppe mit nur einem Stand: Er ist selbst die Investition und trägt den
        /// Abzug (A/B-Fall des Auftrags auf einem Einzelprojekt).</summary>
        [Fact]
        public void Ein_einzelner_Stand_traegt_den_Abzug_selbst()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            WirtschaftlichkeitParameter abzug = Satz();
            abzug.RisikoArt = Risikoart.ABZUG;
            abzug.RisikoVerlust = 10000;
            abzug.RisikoWahrscheinlichkeit = 10;

            WirtschaftlichkeitErgebnis v = Lauf(Gruppe(1), Satz()).Single(e => e.Szenario == ERWARTET);
            WirtschaftlichkeitErgebnis n = Lauf(Gruppe(1), abzug).Single(e => e.Szenario == ERWARTET);
            double barwert = 0;
            for (int t = 1; t <= 20; t++) barwert += 1000.0 / Math.Pow(1.03, t);
            Assert.Equal(Kw(v) - barwert, Kw(n), 4);
        }

        /// <summary>Der Verlauf rechnet denselben Abzug: Die Linie der Variante endet um den
        /// Barwert der Abzüge tiefer, die Referenz bleibt; die Gliederung weist den Abzug als
        /// eigenen Bestandteil aus und geht auf.</summary>
        [Fact]
        public void Verlauf_und_Gliederung_tragen_denselben_Abzug()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            WirtschaftlichkeitParameter abzug = Satz();
            abzug.RisikoArt = Risikoart.ABZUG;
            abzug.RisikoVerlust = 10000;
            abzug.RisikoWahrscheinlichkeit = 10;

            var ctrl = new WirtschaftlichkeitCtrl();
            WirtschaftlichkeitVerlauf vorher = ctrl.BerechneVerlauf(Gruppe(2), Satz(), 20, ERWARTET);
            WirtschaftlichkeitVerlauf nachher = ctrl.BerechneVerlauf(Gruppe(2), abzug, 20, ERWARTET);

            VerlaufSerie sv = vorher.Absolut.Single(s => s.IdProjekt == VARIANTE);
            VerlaufSerie sn = nachher.Absolut.Single(s => s.IdProjekt == VARIANTE);
            double barwert = 0;
            for (int t = 1; t <= 20; t++) barwert += 1000.0 / Math.Pow(1.03, t);
            Assert.Equal(sv.Kumuliert[20] - barwert, sn.Kumuliert[20], 4);
            Assert.Equal(sv.Kumuliert[0], sn.Kumuliert[0]);
            Assert.Equal(nachher.Absolut.Single(s => s.IdProjekt == STAMM).Kumuliert[20],
                         vorher.Absolut.Single(s => s.IdProjekt == STAMM).Kumuliert[20]);

            Zahlungsgliederung g = Zahlungsgliederung.Aus(sn.Bild, 3.0);
            Assert.True(g.Stimmig);
            Assert.Equal(Zahlungsgliederung.RISIKO, g.Schluessel[5]);
            Assert.Equal(-barwert, g.Bestandteil(Zahlungsgliederung.RISIKO).Barwert, 4);
            Zahlungsgliederung gs = Zahlungsgliederung.Aus(
                nachher.Absolut.Single(s => s.IdProjekt == STAMM).Bild, 3.0);
            Assert.Null(gs.Bestandteil(Zahlungsgliederung.RISIKO));
            Assert.Same(Zahlungsgliederung.Reihenfolge, gs.Schluessel);
            Zahlungsgliederung d = Zahlungsgliederung.Differenz(g, gs);
            Assert.Equal(-barwert, d.Bestandteil(Zahlungsgliederung.RISIKO).Barwert, 4);

            // Die Mehrjahrestabelle führt die Spalte und prüft sich selbst.
            Mehrjahresbild m = Mehrjahresbild.Baue(sn);
            MehrjahresSpalte risiko = m.Spalten.Single(s => s.Schluessel == Mehrjahresbild.RISIKO);
            Assert.Equal(0.0, risiko.Wert(0));
            Assert.Equal(-1000.0, risiko.Wert(1));
            MehrjahresSpalte netto = m.Spalten.Single(s => s.Schluessel == "NETTO");
            for (int t = 0; t <= m.Jahre; t++)
                Assert.Equal(netto.Wert(t), m.Spalten.Where(s => !s.IstSumme).Sum(s => s.Wert(t)), 6);
            Assert.Null(Mehrjahresbild.Baue(nachher.Absolut.Single(s => s.IdProjekt == STAMM))
                                      .Spalten.FirstOrDefault(s => s.Schluessel == Mehrjahresbild.RISIKO));
        }

        // =====================================================================
        // Speicherweg
        // =====================================================================

        /// <summary>
        /// Die vier Größen gehen hin und zurück — über den INSERT-Weg (Projekt ohne Zeile) und
        /// den UPDATE-Weg; die Art wird normiert gespeichert, „aus" und leere Zahlen bleiben NULL.
        /// </summary>
        [Fact]
        public void Das_Risiko_ueberlebt_Speichern_und_Laden()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            foreach (int projekt in new[] { PROJEKT_OHNE_ZEILE, PROJEKT_MIT_ZEILE })
            {
                var ctrl = new WirtschaftlichkeitCtrl();
                WirtschaftlichkeitParameter p = ctrl.LadeParameter(projekt);
                Assert.Null(p.RisikoArt);
                Assert.Null(p.RisikoZinszuschlag);
                Assert.Null(p.RisikoVerlust);
                Assert.Null(p.RisikoWahrscheinlichkeit);

                p.RisikoArt = "abzug";
                p.RisikoZinszuschlag = 1.5;
                p.RisikoVerlust = 10000;
                p.RisikoWahrscheinlichkeit = 10;
                Assert.True(ctrl.SpeichereParameter(p), "Speichern " + projekt + ": " + ctrl.Speicherfehler);

                WirtschaftlichkeitParameter z = new WirtschaftlichkeitCtrl().LadeParameter(projekt);
                Assert.Equal(Risikoart.ABZUG, z.RisikoArt);
                Assert.Equal(1.5, z.RisikoZinszuschlag);
                Assert.Equal(10000.0, z.RisikoVerlust);
                Assert.Equal(10.0, z.RisikoWahrscheinlichkeit);
                Assert.Equal(p.Zinssatz, z.Zinssatz);

                z.RisikoArt = null;
                z.RisikoWahrscheinlichkeit = null;
                Assert.True(new WirtschaftlichkeitCtrl().SpeichereParameter(z));
                WirtschaftlichkeitParameter z2 = new WirtschaftlichkeitCtrl().LadeParameter(projekt);
                Assert.Null(z2.RisikoArt);
                Assert.Null(z2.RisikoWahrscheinlichkeit);
                Assert.Equal(10000.0, z2.RisikoVerlust);
                Assert.Equal(1.5, z2.RisikoZinszuschlag);
            }
        }

        // =====================================================================
        // Ausweis nur bei Pflege
        // =====================================================================

        /// <summary>
        /// Nachweiszeile, Szenariozeile, Annahmentafel und Deklaration nennen das Risiko nur,
        /// wenn es gepflegt ist — ohne Pflege bleibt jede Zeile, wie sie war.
        /// </summary>
        [Fact]
        public void Der_Ausweis_nennt_das_Risiko_nur_bei_Pflege()
        {
            WirtschaftlichkeitParameter aus = Satz();
            string zeile = aus.Nachweis(DE);
            string szenario = aus.SatzBest.Nachweis(aus, DE);
            List<AnnahmeZeile> annahmen = ValeriAusweis.Annahmen(aus, DE);
            Assert.DoesNotContain(annahmen, a => a.Schluessel == AnnahmeZeile.RISIKO);
            Assert.Equal(R.WIRT_DEKL_RISIKO_OHNE_NM,
                         ValeriAusweis.Deklarationen(null, aus).Single(d => d.Schluessel == ValeriDeklaration.RISIKO).Text);

            WirtschaftlichkeitParameter abzug = Satz();
            abzug.RisikoArt = Risikoart.ABZUG;
            abzug.RisikoVerlust = 10000;
            abzug.RisikoWahrscheinlichkeit = 10;
            string anhang = RisikoModul.Nachweis(abzug, DE);
            Assert.Contains("10.000", anhang);
            Assert.Contains("1.000", anhang);
            Assert.Equal(zeile + anhang, abzug.Nachweis(DE));
            Assert.Equal(szenario + anhang, abzug.SatzBest.Nachweis(abzug, DE));
            // Die Herleitungszeile des Dialogs nennt das Risiko in der eigenen Gruppe.
            Assert.DoesNotContain(anhang, abzug.SatzBest.Nachweis(abzug, DE, true));

            AnnahmeZeile r = ValeriAusweis.Annahmen(abzug, DE).Single(a => a.Schluessel == AnnahmeZeile.RISIKO);
            Assert.Equal(R.WIRT_ANN_RISIKO, r.Groesse);
            Assert.Equal(r.Erwartet, r.Guenstig);
            Assert.Equal(r.Erwartet, r.Unguenstig);
            Assert.True(r.Gepflegt);

            string dekl = ValeriAusweis.Deklarationen("Versorgungssicherheit", abzug)
                                       .Single(d => d.Schluessel == ValeriDeklaration.RISIKO).Text;
            Assert.StartsWith(R.WIRT_DEKL_RISIKO_ANGESETZT.Substring(0, R.WIRT_DEKL_RISIKO_ANGESETZT.IndexOf('{')), dekl);
        }

        // =====================================================================
        // Formelmappe (Stufe 0 und 1)
        // =====================================================================

        /// <summary>Ohne Risiko ist der Parameterblock der von vorher: gleiche Höhe, Zins_i
        /// an der Zinszeile, kein Risikoname.</summary>
        [Fact]
        public void Formelmappe_ohne_Risiko_bleibt_der_Parameterblock()
        {
            using var wb = new XLWorkbook();
            IXLWorksheet ws = wb.AddWorksheet("Wirtschaftlichkeit");
            int naechste = ExcelFormelmappe.Parameterblock(ws, 1, Satz(), null, new Formelregister());

            Assert.Equal(1 + ExcelFormelmappe.PARAMETERBLOCK_ZEILEN, naechste);
            Assert.Equal(2, wb.DefinedNames.Single(n => n.Name == ExcelFormelmappe.ZINS).Ranges.First().FirstCell().Address.RowNumber);
            Assert.DoesNotContain(wb.DefinedNames, n => n.Name.StartsWith("Risiko_", StringComparison.Ordinal));
            Assert.DoesNotContain(wb.DefinedNames, n => n.Name == ExcelFormelmappe.ZINS_BASIS);
        }

        /// <summary>
        /// ZINSZUSCHLAG in der Mappe: „Kalkulationszins" heißt Zins_Basis, darunter stehen der
        /// Zuschlag und der Zins mit Zuschlag als FORMEL mit dem Namen Zins_i — Wertfassung
        /// gleich Formelfassung in allen drei Szenariospalten.
        /// </summary>
        [Fact]
        public void Formelmappe_Zinszuschlag_rechnet_Zins_i_als_Formel()
        {
            WirtschaftlichkeitParameter p = Satz();
            p.RisikoArt = Risikoart.ZINS;
            p.RisikoZinszuschlag = 1.0;

            using var wb = new XLWorkbook();
            IXLWorksheet ws = wb.AddWorksheet("Wirtschaftlichkeit");
            var register = new Formelregister();
            int naechste = ExcelFormelmappe.Parameterblock(ws, 1, p, null, register);

            Assert.Equal(1 + ExcelFormelmappe.PARAMETERBLOCK_ZEILEN + 2, naechste);
            Assert.Equal(0, register.Abweichungen);
            Assert.Equal(R.WIRT_FM_PARAM_ZINS, ws.Cell(2, 1).GetString());
            Assert.Equal(R.WIRT_FM_PARAM_RISIKO_ZUSCHLAG, ws.Cell(3, 1).GetString());
            Assert.Equal(R.WIRT_FM_PARAM_ZINS_RISIKO, ws.Cell(4, 1).GetString());
            Assert.Equal(ExcelFormelmappe.ZINS_BASIS, ws.Cell(2, 5).GetString());
            Assert.Equal(ExcelFormelmappe.ZINS, ws.Cell(4, 5).GetString());

            string[] szenarien = { ERWARTET, BEST, WORST };
            for (int c = 2; c <= 4; c++)
            {
                Assert.Equal("B2+B3".Replace('B', (char)('A' + c - 1)), ws.Cell(4, c).FormulaA1);
                double gerechnet = p.FuerSzenario(szenarien[c - 2]).Zinssatz / 100.0;
                Assert.Equal(gerechnet, ws.Cell(4, c).GetDouble(), 12);     // ClosedXML rechnet die Formel
            }
        }

        /// <summary>
        /// ZAHLUNGSSTROMABZUG in der Mappe: R_loss, p_loss und der Abzug je Periode als Formel
        /// (Stufe 0); die Risikospalte der Mehrjahrestabelle trägt =-Risiko_Abzug (Stufe 1),
        /// und jede Formel besteht die Gegenrechnung.
        /// </summary>
        [Fact]
        public void Formelmappe_Abzug_rechnet_in_Formeln()
        {
            WirtschaftlichkeitParameter p = Satz();
            p.RisikoArt = Risikoart.ABZUG;
            p.RisikoVerlust = 10000;
            p.RisikoWahrscheinlichkeit = 10;

            using var wb = new XLWorkbook();
            IXLWorksheet ws = wb.AddWorksheet("Wirtschaftlichkeit");
            var register = new Formelregister();
            int naechste = ExcelFormelmappe.Parameterblock(ws, 1, p, null, register);
            Assert.Equal(1 + ExcelFormelmappe.PARAMETERBLOCK_ZEILEN + 3, naechste);

            IXLCell abzug = wb.DefinedNames.Single(n => n.Name == ExcelFormelmappe.RISIKO_ABZUG).Ranges.First().FirstCell();
            Assert.False(string.IsNullOrEmpty(abzug.FormulaA1));
            Assert.Equal(1000.0, abzug.GetDouble(), 9);
            Assert.Equal(R.WIRT_FM_PARAM_RISIKO_ABZUG, ws.Cell(abzug.Address.RowNumber, 1).GetString());

            // Stufe 1: die Mehrjahrestabelle eines Standes mit Abzug.
            KapitalwertRechner.Zahlungsbild bild = Bild(RisikoModul.AbzugJeJahr(p));
            var serie = new VerlaufSerie { IdProjekt = VARIANTE, Bild = bild, RestwertBarwert = bild.RestwertBarwert };
            serie.Kumuliert = new double[bild.BarwertReihe.Length];
            double kum = 0;
            for (int t = 0; t < bild.BarwertReihe.Length; t++) { kum += bild.BarwertReihe[t]; serie.Kumuliert[t] = kum; }
            Mehrjahresbild m = Mehrjahresbild.Baue(serie);

            int kopf = naechste + 1;
            MehrjahresTafel tafel = ExcelFormelmappe.Mehrjahrestabelle(ws, kopf, VARIANTE, m, serie, p, register);
            Assert.NotNull(tafel);
            int spalte = ExcelFormelmappe.SpalteVon(m, Mehrjahresbild.RISIKO);
            Assert.True(spalte > 0);
            Assert.Equal("-" + ExcelFormelmappe.RISIKO_ABZUG, ws.Cell(tafel.Zeile(1), spalte).FormulaA1);
            Assert.Equal("-" + ExcelFormelmappe.RISIKO_ABZUG, ws.Cell(tafel.Zeile(tafel.Jahre), spalte).FormulaA1);
            Assert.Equal(-1000.0, ws.Cell(tafel.Zeile(1), spalte).GetDouble(), 9);
            Assert.Equal(0, register.Abweichungen);
        }

        // =====================================================================
        // Hilfsmittel
        // =====================================================================

        /// <summary>i = 3 %, T = 20 a, p_E 2 %/a, p_B 1 %/a.</summary>
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

        /// <summary>Ein festes Zahlungsbild: 100.000 € Investition (Nutzungsdauer 25 a, also
        /// Restwert), Betrieb 2.000, Energie 10.000, Erlös 15.000 €/a.</summary>
        private static KapitalwertRechner.Zahlungsbild Bild(double abzug, bool ohneParameter = false)
        {
            var invest = new List<KapitalwertRechner.InvestPosition>
            {
                new KapitalwertRechner.InvestPosition { Betrag = 100000, Nutzungsdauer = 25 }
            };
            return ohneParameter
                ? KapitalwertRechner.Rechne(invest, 2000, 10000, 15000, 3.0, 20, 1.0, 2.0)
                : KapitalwertRechner.Rechne(invest, 2000, 10000, 15000, 3.0, 20, 1.0, 2.0,
                                            risikoAbzugJahr: abzug);
        }

        /// <summary>Eine synthetische Gruppe ohne Zeilen in <c>Tab_Projekt</c> (Muster
        /// <c>SzenarioParameterTests.Pruefstand</c>): Stamm mit 10.000 €/a Energiekosten, auf
        /// Wunsch eine Variante mit 8.000 €/a; beide mit 100 MWh PV-Überschuss.</summary>
        private static BerichtsDaten Gruppe(int staende)
        {
            var daten = new BerichtsDaten { IdStamm = STAMM, Stammprojektname = "Prüfstand E15" };
            daten.Varianten.Add(Stand(STAMM, true, 10000.0));
            if (staende > 1) daten.Varianten.Add(Stand(VARIANTE, false, 8000.0));
            return daten;
        }

        private static VariantenDaten Stand(int id, bool stamm, double energiekosten)
        {
            return new VariantenDaten
            {
                IdProjekt = id,
                IstStamm = stamm,
                Projektname = stamm ? "Prüfstand E15" : "Variante E15",
                Ergebnis = new ErgebnisModel
                {
                    Photovoltaik = new ErgebnisPhotovoltaikModel { Stromproduktion = 150.0, Ueberschuss = 100.0 }
                },
                Energiekosten = energiekosten
            };
        }

        /// <summary>Der Lauf ohne Speichern, in der Reihenfolge Szenario × Stand.</summary>
        private static List<WirtschaftlichkeitErgebnis> Lauf(BerichtsDaten daten, WirtschaftlichkeitParameter p)
        {
            return new WirtschaftlichkeitCtrl().Berechne(daten, p, 0, false);
        }

        private static double Kw(WirtschaftlichkeitErgebnis e)
        {
            Assert.True(e.Kapitalwert.HasValue,
                        "Kein Kapitalwert (" + e.IdProjekt + ", " + e.Szenario + "): " + (e.Fehlgrund ?? e.Hinweis));
            return e.Kapitalwert.Value;
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
