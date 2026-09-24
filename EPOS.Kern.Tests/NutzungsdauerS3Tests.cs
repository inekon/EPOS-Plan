using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EPOS.UI.Dialoge.Kosten;
using Microsoft.Data.Sqlite;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ETAPPE E10 — <b>Stufe S3 des Nutzungsdauer-Konzepts: Instandsetzung und Wartung je
    /// Technik</b> (Empfehlungen E10‑Q1 a, E10‑Q2 a, E10‑Q6 a).
    ///
    /// <para><b>Was hier gehalten wird.</b> Die Saat der zwei Satzspalten aus den Konstanten
    /// der Betriebsvorlagen (keine erfundene Normzahl, ND‑Q8) und ihr Schemaschritt 120
    /// samt Werkzeug und Repo-Datei; der Lese- und Schreibweg der Sätze in Controller und
    /// Hülle des Dialogs „Nutzungsdauern (AfA)"; die Regel für Vorbelegung und Anzeige
    /// (gepflegter Satz vor dem Satz der Tabelle); dass die Tabelle NICHT selbst rechnet
    /// (Fassung E10/9, Anwenderentscheid ND‑Q4) — ihre Sätze wirken allein über die
    /// ausdrückliche Vorbelegung (Vorlagenübernahme, Knopf „Sätze vorbelegen…"; die
    /// Pflichtanlage des Wizards bleibt ergebnisneutral); der neue Kesseleintrag in %/a; und
    /// der Rechenweg eines vorbelegten Satzes samt Herkunft, Herleitung und Formelmappe
    /// (Stufe 3) an Projekt 1018.</para>
    ///
    /// <para><b>Der Träger der Rechenfälle</b> ist das BHKW des Projekts 1018 (Anlage
    /// 11327) — dieselbe Anlage wie in <see cref="BetriebskostenBasisTests"/>. Seine
    /// Betriebsvorlage trägt vier Zeilen „Instandhaltung …" mit „% der Investition" ohne
    /// Satz und ohne Betrag; sie sind genau die Zeilen, die S3 erreicht.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class NutzungsdauerS3Tests
    {
        // ---- Standardzeilen der Saat (Tab_Nutzungsdauer.ID) --------------------------
        private const int ZEILE_KESSEL = 1;              // Heizkessel · Wärmeerzeuger
        private const int ZEILE_BHKW = 6;                // BHKW · Modul
        private const int ZEILE_BATTERIE = 20;           // Stromspeicher · Batterie
        private const int ZEILE_WAERMEZENTRALE = 23;     // Wärmezentrale · Rohrleitungen
        private const int ZEILE_STROMEINSPEISUNG = 25;   // Stromeinspeisung · Netzanschluss
        private const int ZEILE_BAULICH = 26;            // Bauliche Anlagen

        // ---- Projekt 1018, BHKW-Anlage 11327 -----------------------------------------
        private const int PROJEKT = 1018;
        private const int BHKW = 7;                      // Tab_KostenKomponente.ID
        private const int ANLAGE = 11327;
        private const int B_BHKW = 101600555;            // Instandhaltung BHKW
        private const int B_WAERMEZENTRALE = 101600557;  // Instandhaltung Wärmezentrale
        private const int B_BAULICH = 101600558;         // Instandhaltung bauliche Anlagen
        private const int B_STROMEINSPEISUNG = 101600559; // Instandhaltung Stromeinspeisung
        private const int B_PERSONAL = 101600560;        // Personalkosten — keine Zuordnung

        /// <summary>Das leere Projekt der Testdatenbank für die Vorlagenübernahme.</summary>
        private const int LEERES_PROJEKT = 1006;

        // =====================================================================
        //  Die Saat — aus den Konstanten der Vorlagen
        // =====================================================================

        /// <summary>
        /// Gesät wird die MITTE des Empfehlungsbereichs derselben Position in einer
        /// Betriebsvorlage — fünf Sätze, alle Instandsetzung. Wartung ist in keiner Vorlage
        /// ein Prozentsatz und bleibt deshalb leer; wo die Vorlage keinen Bereich kennt
        /// (Wärmepumpe, Solarthermie, Speicher, Photovoltaik), bleibt die Technik leer.
        /// </summary>
        [Fact]
        public void Die_Saat_ist_die_Mitte_der_Empfehlungsbereiche_der_Betriebsvorlagen()
        {
            IReadOnlyList<BetriebssatzSaat> saat = NutzungsdauerSaetze.Saat;

            Assert.Equal(5, saat.Count);
            Assert.All(saat, s => Assert.Equal(Satzart.Instandsetzung, s.Art));

            Assert.Equal(2.0, NutzungsdauerSaetze.SaatSatz(2, Satzart.Instandsetzung));    // 1,5 … 2,5
            Assert.Equal(6.0, NutzungsdauerSaetze.SaatSatz(7, Satzart.Instandsetzung));    // 3 … 9
            Assert.Equal(2.0, NutzungsdauerSaetze.SaatSatz(8, Satzart.Instandsetzung));    // 1,8 … 2,2
            Assert.Equal(1.25, NutzungsdauerSaetze.SaatSatz(9, Satzart.Instandsetzung));   // 1,0 … 1,5
            Assert.Equal(2.0, NutzungsdauerSaetze.SaatSatz(10, Satzart.Instandsetzung));   // 1,8 … 2,2

            foreach (int ohne in new[] { 1, 3, 4, 5, 6 })
                Assert.Null(NutzungsdauerSaetze.SaatSatz(ohne, Satzart.Instandsetzung));
            for (int technik = 1; technik <= 10; technik++)
                Assert.Null(NutzungsdauerSaetze.SaatSatz(technik, Satzart.Wartung));

            // Jede Saatzahl steht so in der Vorlagen-Saat — keine erfundene Normzahl (ND‑Q8).
            foreach (BetriebssatzSaat s in saat)
            {
                SchemaKatalog.VorlagenPositionSeed p = SchemaKatalog.Schritt39_Vorlagen
                    .Where(v => v.KategorieId == DbWerte.KOSTEN_KATEGORIE_BETRIEB &&
                                v.Komponente == s.Quellvorlage)
                    .SelectMany(v => v.Positionen)
                    .Single(x => x.Bezeichnung == s.Quellposition);
                Assert.Equal(DbWerte.BEMESSUNG_PROZENT_INVESTITION, p.Bemessung);
                Assert.Equal(p.EmpfehlungVon, s.Von);
                Assert.Equal(p.EmpfehlungBis, s.Bis);
                Assert.Equal((p.EmpfehlungVon.Value + p.EmpfehlungBis.Value) / 2.0, s.Satz, 9);
            }

            // Bevorzugt die Vorlage der Technik selbst: „Instandhaltung Heizkessel" steht in
            // der BHKW- und in der Kesselvorlage — gesät wird aus der des Kessels.
            Assert.Equal(DbWerte.KOSTEN_KOMPONENTE_HEIZKESSEL,
                         saat.Single(s => s.KomponentenId == 2).Quellvorlage);
        }

        /// <summary>
        /// Die Zuordnung der Betriebspositionen: „Instandhaltung …" nimmt die
        /// Instandsetzung, „Wartung …" die Wartung, „Personalkosten" nichts; eine Position,
        /// die in der Vorlage einer ANDEREN Technik steht, nimmt die Technik ihres Namens.
        /// </summary>
        [Fact]
        public void Die_Zuordnung_kennt_die_Positionen_der_Betriebsvorlagen()
        {
            BetriebssatzZuordnung waermezentrale =
                NutzungsdauerSaetze.ZuordnungZu(DbWerte.VDI_POS_INSTANDHALTUNG_WAERMEZENTRALE);
            Assert.NotNull(waermezentrale);
            Assert.Equal(8, waermezentrale.KomponentenId);
            Assert.Equal(Satzart.Instandsetzung, waermezentrale.Art);

            BetriebssatzZuordnung wartung = NutzungsdauerSaetze.ZuordnungZu(DbWerte.VDI_POS_WARTUNG_BHKW);
            Assert.NotNull(wartung);
            Assert.Equal(Satzart.Wartung, wartung.Art);

            Assert.Null(NutzungsdauerSaetze.ZuordnungZu("Personalkosten"));
            Assert.Null(NutzungsdauerSaetze.ZuordnungZu("Steuern, Versicherung, Verwaltung"));
            Assert.Null(NutzungsdauerSaetze.ZuordnungZu(""));

            // Randleerzeichen stören nicht, Groß-/Kleinschreibung schon (Positionsschlüssel).
            Assert.NotNull(NutzungsdauerSaetze.ZuordnungZu(" " + DbWerte.VDI_POS_INSTANDHALTUNG_BHKW + " "));
            Assert.Null(NutzungsdauerSaetze.ZuordnungZu(DbWerte.VDI_POS_INSTANDHALTUNG_BHKW.ToUpperInvariant()));

            Assert.True(NutzungsdauerSatzCtrl.Satzfaehig(DbWerte.BEMESSUNG_PROZENT_INVESTITION,
                                                         DbWerte.VDI_POS_INSTANDHALTUNG_BHKW));
            Assert.False(NutzungsdauerSatzCtrl.Satzfaehig(DbWerte.BEMESSUNG_BETRAG,
                                                          DbWerte.VDI_POS_INSTANDHALTUNG_BHKW));
            Assert.False(NutzungsdauerSatzCtrl.Satzfaehig(DbWerte.BEMESSUNG_PROZENT_INVESTITION,
                                                          "Personalkosten"));
        }

        // =====================================================================
        //  Schritt 120 — Nachsaat, Werkzeug, Repo-Datei
        // =====================================================================

        /// <summary>
        /// Die nachgezogene Arbeitskopie trägt genau die fünf Sätze; sonst steht keine
        /// Satzzelle. Der Schritt ist wiederholbar: Ein zweiter Lauf setzt nichts.
        /// </summary>
        [Fact]
        public void Schritt_120_saet_die_fuenf_Standardzeilen_und_ist_wiederholbar()
        {
            Assert.True(SchemaStand.Zielversion >= 120,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter 120.");
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            NutzungsdauerCtrl.ProbeVergessen();

            Assert.Equal(0, NutzungsdauerSaetze.Offen());
            Assert.Equal(2.0, Instandsetzung(ZEILE_KESSEL));
            Assert.Equal(6.0, Instandsetzung(ZEILE_BHKW));
            Assert.Equal(2.0, Instandsetzung(ZEILE_WAERMEZENTRALE));
            Assert.Equal(2.0, Instandsetzung(ZEILE_STROMEINSPEISUNG));
            Assert.Equal(1.25, Instandsetzung(ZEILE_BAULICH));
            Assert.Null(Instandsetzung(ZEILE_BATTERIE));

            Assert.Equal(5, Zahl("SELECT COUNT(*) FROM Tab_Nutzungsdauer WHERE Instandsetzung_Prozent IS NOT NULL"));
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM Tab_Nutzungsdauer WHERE Wartung_Prozent IS NOT NULL"));

            NutzungsdauerSaetze.Bericht zweiter = NutzungsdauerSaetze.Ausfuehren();
            Assert.Equal(0, zweiter.Gesetzt);
            Assert.Equal(5, zweiter.Belegt);
            Assert.Equal(0, zweiter.OhneZeile);
        }

        /// <summary>
        /// Die Nachsaat füllt NUR leere Zellen: Ein gepflegter Satz bleibt stehen, eine
        /// geleerte Zelle bekommt ihren Saatsatz zurück.
        /// </summary>
        [Fact]
        public void Die_Nachsaat_fuellt_nur_leere_Zellen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            NutzungsdauerCtrl.ProbeVergessen();

            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_Nutzungsdauer SET Instandsetzung_Prozent = NULL WHERE ID = " + ZEILE_KESSEL);
            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_Nutzungsdauer SET Instandsetzung_Prozent = 7.5 WHERE ID = " + ZEILE_BHKW);
            Assert.Equal(1, NutzungsdauerSaetze.Offen());

            NutzungsdauerSaetze.Bericht b = NutzungsdauerSaetze.Ausfuehren();

            Assert.Equal(1, b.Gesetzt);
            Assert.Equal(4, b.Belegt);
            Assert.Equal(2.0, Instandsetzung(ZEILE_KESSEL));
            Assert.Equal(7.5, Instandsetzung(ZEILE_BHKW));
            Assert.Equal(0, NutzungsdauerSaetze.Offen());
        }

        /// <summary>
        /// <b>Die Werkzeug-Wache des Schritts 120.</b> Die REPO-Datei trägt die fünf Sätze
        /// (gelesen nur lesend und ohne Spuren, wie <see cref="TestdatenbankSchemastandWacheTests"/>),
        /// und alle drei Wege führen den Schritt: Migration der Schale, Werkzeug
        /// <c>Testdatenbankschema</c> und die Nachzieh-Liste der Tests.
        /// </summary>
        [Fact]
        public void Repo_Datei_Werkzeug_und_Migration_fuehren_den_Schritt_120()
        {
            string wurzel = Repowurzel();
            if (wurzel == null) return;

            string werkzeug = File.ReadAllText(Path.Combine(wurzel, "Werkzeuge", "Testdatenbankschema", "Program.cs"));
            Assert.Contains("NutzungsdauerSaetze.Ausfuehren()", werkzeug);
            string migration = File.ReadAllText(Path.Combine(wurzel, "WindowsFormsApplication1", "Allgemein",
                                                             "Update", "SchemaMigration.cs"));
            Assert.Contains("Schritt_120_NutzungsdauerSaetze", migration);
            Assert.Contains("NutzungsdauerSaetze.Offen()", migration);
            string vorrichtung = File.ReadAllText(Path.Combine(wurzel, "EPOS.Kern.Tests", "TestDatenbank.cs"));
            Assert.Contains("NutzungsdauerSaetze.Ausfuehren()", vorrichtung);

            string pfad = Path.Combine(wurzel, "Referenzlaeufe", "Kenndaten_Test.sqlite");
            if (!File.Exists(pfad)) return;
            LfsZeigerProbe.Sicherstellen(pfad);

            string uri = "file:" + pfad.Replace('\\', '/').Replace("?", "%3f") + "?mode=ro&immutable=1";
            using var verbindung = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = uri }.ToString());
            verbindung.Open();

            Assert.Equal(2.0, RepoSatz(verbindung, ZEILE_KESSEL));
            Assert.Equal(6.0, RepoSatz(verbindung, ZEILE_BHKW));
            Assert.Equal(2.0, RepoSatz(verbindung, ZEILE_WAERMEZENTRALE));
            Assert.Equal(2.0, RepoSatz(verbindung, ZEILE_STROMEINSPEISUNG));
            Assert.Equal(1.25, RepoSatz(verbindung, ZEILE_BAULICH));
            Assert.Null(RepoSatz(verbindung, ZEILE_BATTERIE));
        }

        // =====================================================================
        //  Sichtbar und gespeichert — Controller und Hülle des Dialogs
        // =====================================================================

        /// <summary>
        /// Eine neue Zeile trägt ihre Sätze; eine Auslieferungszeile nimmt geänderte Sätze
        /// an, und „Auslieferungswerte wiederherstellen" setzt sie auf die Saat zurück — die
        /// eigene Zeile bleibt unberührt.
        /// </summary>
        [Fact]
        public void Saetze_werden_gespeichert_und_wiederhergestellt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            NutzungsdauerCtrl.ProbeVergessen();

            int id = NutzungsdauerCtrl.Neu(BHKW, "E10 Probeart", 12.0, null, 3.5, 1.25,
                                           "eigener Wert", out string grund);
            Assert.True(id > 0, grund);
            NutzungsdauerZeile neu = NutzungsdauerCtrl.Zeile(id);
            Assert.Equal(3.5, neu.InstandsetzungProzent);
            Assert.Equal(1.25, neu.WartungProzent);

            NutzungsdauerZeile bhkw = NutzungsdauerCtrl.Zeile(ZEILE_BHKW);
            bhkw.InstandsetzungProzent = 4.0;
            bhkw.WartungProzent = 1.0;
            Assert.True(NutzungsdauerCtrl.Speichern(bhkw, out grund), grund);
            Assert.Equal(4.0, NutzungsdauerCtrl.Zeile(ZEILE_BHKW).InstandsetzungProzent);
            Assert.Equal(1.0, NutzungsdauerCtrl.Zeile(ZEILE_BHKW).WartungProzent);

            Assert.True(NutzungsdauerCtrl.AuslieferungWiederherstellen() > 0);
            Assert.Equal(6.0, NutzungsdauerCtrl.Zeile(ZEILE_BHKW).InstandsetzungProzent);
            Assert.Null(NutzungsdauerCtrl.Zeile(ZEILE_BHKW).WartungProzent);
            Assert.Equal(3.5, NutzungsdauerCtrl.Zeile(id).InstandsetzungProzent);
            Assert.Equal(1.25, NutzungsdauerCtrl.Zeile(id).WartungProzent);
        }

        /// <summary>
        /// Die Hülle des Dialogs zeigt die Sätze und schreibt sie — über „Speichern" und
        /// über die Neuzeile.
        /// </summary>
        [Fact]
        public void Die_Huelle_des_Dialogs_zeigt_und_schreibt_die_Saetze()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            NutzungsdauerCtrl.ProbeVergessen();

            IReadOnlyDictionary<string, object> gaben = new NutzungsdauerHuelle().Gaben();
            var zeilen = (IReadOnlyList<NutzungsdauerZeileAnzeige>)gaben["Zeilen"];
            NutzungsdauerZeileAnzeige bhkw = zeilen.Single(z => z.Id == ZEILE_BHKW);
            Assert.Equal(6.0, bhkw.InstandsetzungProzent);
            Assert.Null(bhkw.WartungProzent);

            bhkw.WartungProzent = 1.5;
            var speichern = (Func<IReadOnlyList<NutzungsdauerZeileAnzeige>, string>)gaben["Speichern"];
            Assert.Null(speichern(new[] { bhkw }));
            Assert.Equal(1.5, NutzungsdauerCtrl.Zeile(ZEILE_BHKW).WartungProzent);

            var anlegen = (Func<NutzungsdauerNeuEingabe, string>)gaben["AnlegenDelegat"];
            Assert.Null(anlegen(new NutzungsdauerNeuEingabe(BHKW, "E10 Neuzeile", 12.0, null, 3.5, 0.75)));
            NutzungsdauerZeile neu = NutzungsdauerCtrl.Alle().Single(z => z.Positionsart == "E10 Neuzeile");
            Assert.Equal(3.5, neu.InstandsetzungProzent);
            Assert.Equal(0.75, neu.WartungProzent);
        }

        // =====================================================================
        //  Der Satz für Vorbelegung und Anzeige — gepflegt vor Tabelle
        // =====================================================================

        /// <summary>
        /// Die Regel an einem Lesestand ohne Datenbank (Fassung E10/9): Ein gepflegter Satz
        /// bleibt — auch 0; eine leere, satzfähige Position bekommt den Satz der Tabelle als
        /// Wert der Vorbelegung; ist der gepflegte Satz genau der der Tabelle, trägt er ihre
        /// Herkunft. Andere Bemessung oder keine Zuordnung: kein Tabellensatz.
        /// </summary>
        [Fact]
        public void Der_Satz_fuer_Vorbelegung_und_Anzeige_folgt_gepflegt_vor_Tabelle()
        {
            NutzungsdauerSatztafel tafel = Probetafel();
            string pinv = DbWerte.BEMESSUNG_PROZENT_INVESTITION;
            string bhkw = DbWerte.VDI_POS_INSTANDHALTUNG_BHKW;

            // Gepflegt bleibt — ohne Herkunft, wo er von der Tabelle abweicht.
            Assert.Equal(3.0, NutzungsdauerSatzCtrl.WirksamerSatz(pinv, 3.0, BHKW, bhkw,
                                                                  ref tafel, out BetriebssatzVorgabe h1));
            Assert.Null(h1);
            Assert.Equal(0.0, NutzungsdauerSatzCtrl.WirksamerSatz(pinv, 0.0, BHKW, bhkw, ref tafel, out _));

            // Gepflegt GLEICH der Tabelle → mit Herkunft.
            Assert.Equal(6.0, NutzungsdauerSatzCtrl.WirksamerSatz(pinv, 6.0, BHKW, bhkw,
                                                                  ref tafel, out BetriebssatzVorgabe h2));
            Assert.Equal(ZEILE_BHKW, h2.Quellzeile.Id);

            // Leer → der Satz der Tabelle, den die Vorbelegung schreibt.
            Assert.Equal(6.0, NutzungsdauerSatzCtrl.WirksamerSatz(pinv, null, BHKW, bhkw,
                                                                  ref tafel, out BetriebssatzVorgabe h3));
            Assert.Equal(Satzart.Instandsetzung, h3.Art);

            // Andere Bemessung oder keine Zuordnung → kein Tabellensatz.
            Assert.Null(NutzungsdauerSatzCtrl.WirksamerSatz(DbWerte.BEMESSUNG_BETRAG, null, BHKW, bhkw,
                                                            ref tafel, out _));
            Assert.Null(NutzungsdauerSatzCtrl.WirksamerSatz(pinv, null, BHKW, "Personalkosten",
                                                            ref tafel, out _));

            // Technikübergreifende Position: „Instandhaltung Wärmezentrale" in der BHKW-Vorlage
            // nimmt den Satz der Wärmezentrale, nicht den des Moduls.
            Assert.Equal(2.0, NutzungsdauerSatzCtrl.WirksamerSatz(
                pinv, null, BHKW, DbWerte.VDI_POS_INSTANDHALTUNG_WAERMEZENTRALE, ref tafel, out _));

            // Die Herkunft, nach der die Nachweisliste fragt: nur ein gepflegter Satz, der
            // genau der der Tabelle ist.
            Assert.True(NutzungsdauerSatzCtrl.AusTabelle(pinv, 6.0, BHKW, bhkw, ref tafel));
            Assert.False(NutzungsdauerSatzCtrl.AusTabelle(pinv, 4.0, BHKW, bhkw, ref tafel));
            Assert.False(NutzungsdauerSatzCtrl.AusTabelle(pinv, null, BHKW, bhkw, ref tafel));
            Assert.False(NutzungsdauerSatzCtrl.AusTabelle(pinv, 6.0, BHKW, "Personalkosten", ref tafel));
        }

        /// <summary>
        /// Positionsart zuerst, sonst die Standardzeile; eine Position ohne eigene Technik
        /// („Wartung / Sichtprüfung Speicher") nimmt die Technik ihrer Komponente; ohne Satz
        /// in der Tabelle gibt es keinen.
        /// </summary>
        [Fact]
        public void Die_Tafel_waehlt_Positionsart_vor_Standardzeile()
        {
            NutzungsdauerSatztafel tafel = Probetafel();

            // Erdsonden tragen einen eigenen Satz …
            Assert.Equal(1.5, tafel.Vorgabe(1, "Instandhaltung Umweltwärmequelle").Satz);
            // … die Wärmepumpe selbst nimmt ihre Standardzeile.
            Assert.Equal(2.5, tafel.Vorgabe(1, "Instandhaltung Wärmepumpe").Satz);

            // Die Speicherwartung nimmt die Technik der Komponente (6 = Pufferspeicher).
            BetriebssatzVorgabe speicher = tafel.Vorgabe(6, "Wartung / Sichtprüfung Speicher");
            Assert.Equal(0.5, speicher.Satz);
            Assert.Equal(Satzart.Wartung, speicher.Art);

            // Ohne Satz an der Standardzeile (Stromspeicher) bleibt es leer.
            Assert.Null(tafel.Vorgabe(5, "Instandhaltung Stromspeicher").Satz);
        }

        /// <summary>
        /// Die Herkunftszeile nennt Satz, Technik, Positionsart und Satzart; sie steht nur,
        /// wenn der gepflegte Satz der der Tabelle ist — ein leeres Feld rechnet mit nichts und
        /// bekommt keine Zeile (Fassung E10/9).
        /// </summary>
        [Fact]
        public void Die_Herkunftszeile_nennt_die_Tabelle()
        {
            using var _ = new Kulturvorrichtung();
            NutzungsdauerSatztafel tafel = Probetafel();
            BetriebssatzVorgabe v = tafel.Vorgabe(BHKW, DbWerte.VDI_POS_INSTANDHALTUNG_BHKW);

            Assert.Equal("6 % · Satz aus Nutzungsdauertabelle: Blockheizkraftwerk · Modul (Instandsetzung)",
                         v.Herleitung);
            Assert.Equal(v.Herleitung, NutzungsdauerSatzCtrl.Herleitungszeile(v, 6.0));
            Assert.Equal("", NutzungsdauerSatzCtrl.Herleitungszeile(v, null));
            Assert.Equal("", NutzungsdauerSatzCtrl.Herleitungszeile(v, 4.0));
            Assert.Equal("Satz aus Nutzungsdauertabelle", NutzungsdauerSatzCtrl.HerkunftKurz());
        }

        // =====================================================================
        //  Vorbelegung
        // =====================================================================

        /// <summary>
        /// Die Vorlagenübernahme setzt den Satz der Tabelle in die frische Projektzeile, wo
        /// die Vorlage keinen trägt — je Position der Satz IHRER Technik.
        /// </summary>
        [Fact]
        public void Die_Vorlagenuebernahme_setzt_den_Satz_der_Tabelle()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            NutzungsdauerCtrl.ProbeVergessen();
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM Tab_ProjektWerte WHERE ProjektID = " + LEERES_PROJEKT));

            KostenVorlageKopf kopf = KostenVorlagenCtrl.Vorlagen(BHKW, DbWerte.KOSTEN_KATEGORIE_BETRIEB)
                                                       .First(k => k.IstStandard);
            UebernahmeErgebnis e = KostenVorlagenUebernahmeCtrl.AusVorlage(LEERES_PROJEKT, kopf);
            Assert.False(e.Fehler, string.Join(" ", e.Meldungen));
            Assert.True(e.Angelegt > 0);

            Assert.Equal(6.0, SatzDerPosition(LEERES_PROJEKT, DbWerte.VDI_POS_INSTANDHALTUNG_BHKW));
            Assert.Equal(2.0, SatzDerPosition(LEERES_PROJEKT, DbWerte.VDI_POS_INSTANDHALTUNG_KESSEL));
            Assert.Equal(2.0, SatzDerPosition(LEERES_PROJEKT, DbWerte.VDI_POS_INSTANDHALTUNG_WAERMEZENTRALE));
            Assert.Equal(1.25, SatzDerPosition(LEERES_PROJEKT, DbWerte.VDI_POS_INSTANDHALTUNG_BAULICH));
            Assert.Equal(2.0, SatzDerPosition(LEERES_PROJEKT, DbWerte.VDI_POS_INSTANDHALTUNG_STROMEINSPEISUNG));
            Assert.Null(SatzDerPosition(LEERES_PROJEKT, "Personalkosten"));
        }

        /// <summary>
        /// Die Pflichtanlage des Wizards (<c>PflichtpositionenSicherstellen</c>) läuft ohne
        /// Zutun des Anwenders und bleibt ergebnisneutral: Auch ihre Zeile „Instandhaltung
        /// Heizkessel" bekommt KEINEN Satz der Tabelle (Fassung E10/9). Projekt 1027 trägt
        /// einen Kessel und eine Wärmepumpe, aber keine einzige Kostenzeile.
        /// </summary>
        [Fact]
        public void Die_Pflichtanlage_schreibt_keinen_Satz_der_Tabelle()
        {
            const int projekt = 1027;
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            NutzungsdauerCtrl.ProbeVergessen();
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM Tab_ProjektWerte WHERE ProjektID = " + projekt));

            Assert.True(KostenVorlagenUebernahmeCtrl.PflichtpositionenSicherstellen(projekt) > 0);

            Assert.True(Zahl("SELECT COUNT(*) FROM Tab_ProjektWerte AS w INNER JOIN Tab_Kostenfaktor AS k " +
                             "ON k.StammID = w.StammID WHERE w.ProjektID = " + projekt +
                             " AND k.Bezeichnung = 'Instandhaltung Heizkessel'") == 1);
            Assert.Null(SatzDerPosition(projekt, DbWerte.VDI_POS_INSTANDHALTUNG_KESSEL));
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM Tab_ProjektWerte WHERE ProjektID = " + projekt +
                                 " AND Einheitpreis IS NOT NULL"));
        }

        /// <summary>
        /// <b>Die bewusste Handlung:</b> Auf der Betriebsseite der Kostenverwaltung steht unter
        /// dem LEEREN Satzfeld keine Herkunft — es rechnet mit nichts. „Sätze vorbelegen…"
        /// füllt die vier leeren Sätze der BHKW-Anlage, die Herkunftszeile erscheint, und
        /// gerechnet wird erst, wenn „Speichern" sie geschrieben hat.
        /// </summary>
        [Fact]
        public void Erst_Vorbelegen_und_Speichern_lassen_die_Saetze_der_Tabelle_rechnen()
        {
            using var _ = new Kulturvorrichtung();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            NutzungsdauerCtrl.ProbeVergessen();
            double basis = BetriebskostenCtrl.InvestSummeFuer(PROJEKT, BHKW, ANLAGE).Value;
            double vorher = WirtschaftlichkeitCtrl.LiesBetriebskosten(PROJEKT, WirtschaftlichkeitSzenario.ERWARTET);

            IReadOnlyDictionary<string, object> gaben =
                KostenKomponenteHuelle.GabenProjekt(PROJEKT, "", null, true, ANLAGE);
            var laden = (Func<KostenKomponenteKontext, KostenKomponenteStand>)gaben["Laden"];
            KostenKomponenteStand stand = laden(new KostenKomponenteKontext((int?)gaben["EintragVorwahl"], false, null));

            Assert.True(stand.SaetzeVorbelegbar);
            KostenPositionZeile bhkw = stand.Zeilen.Single(z => z.Bezeichnung == DbWerte.VDI_POS_INSTANDHALTUNG_BHKW);
            Assert.Null(bhkw.Satz);
            Assert.Equal("", bhkw.SatzHerleitung);

            var vorbelegen = (Func<bool, NutzungsdauerVorbelegung>)gaben["NutzungsdauerVorbelegen"];
            NutzungsdauerVorbelegung v = vorbelegen(false);
            Assert.Equal(4, v.Gefuellt);
            Assert.Equal(0, v.Belegt);
            Assert.Equal(6.0, bhkw.Satz);
            Assert.Contains("Satz aus Nutzungsdauertabelle", bhkw.SatzHerleitung);

            // Noch nichts geschrieben — die Wirtschaftlichkeit steht, wo sie stand.
            Assert.Null(SatzDerZeile(B_BHKW));
            Assert.Equal(vorher, WirtschaftlichkeitCtrl.LiesBetriebskosten(
                PROJEKT, WirtschaftlichkeitSzenario.ERWARTET), 6);

            var speichern = (Func<bool>)gaben["Speichern"];
            Assert.True(speichern());

            Assert.Equal(6.0, SatzDerZeile(B_BHKW));
            Dictionary<int, KostenPositionNachweis> n = Nachweise();
            Pruefe(n[B_BHKW], 6.0, basis);
            Pruefe(n[B_WAERMEZENTRALE], 2.0, basis);
            Pruefe(n[B_BAULICH], 1.25, basis);
            Pruefe(n[B_STROMEINSPEISUNG], 2.0, basis);
            Assert.Equal(vorher + basis * 11.25 / 100.0, WirtschaftlichkeitCtrl.LiesBetriebskosten(
                PROJEKT, WirtschaftlichkeitSzenario.ERWARTET), 6);
        }

        /// <summary>
        /// Gerätekataloge (ND‑Q7 b, E10‑Q2 a): Nur ein NEUER Kesseleintrag in „%/a" ohne
        /// Betrag nimmt den Wartungssatz der Kessel-Standardzeile; ein Betrag bleibt, und
        /// „€/a" hat keinen Gegenwert in der Tabelle.
        /// </summary>
        [Fact]
        public void Ein_neuer_Kesseleintrag_in_Prozent_nimmt_den_Wartungssatz_der_Tabelle()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            NutzungsdauerCtrl.ProbeVergessen();

            // Ausgeliefert ist kein Wartungssatz — dann bleibt es beim leeren Wert.
            var ohneTabelle = new HeizkesselStammCtrl
            {
                Wartungskosten = 0.0, Wartungskosten_Einheit = DbWerte.KESSEL_WARTUNG_EINHEIT_PROZENT
            };
            ohneTabelle.WartungAusNutzungsdauertabelle();
            Assert.Equal(0.0, ohneTabelle.Wartungskosten);

            NutzungsdauerZeile kessel = NutzungsdauerCtrl.Zeile(ZEILE_KESSEL);
            kessel.WartungProzent = 1.5;
            Assert.True(NutzungsdauerCtrl.Speichern(kessel, out string grund), grund);

            var prozent = new HeizkesselStammCtrl
            {
                Wartungskosten = 0.0, Wartungskosten_Einheit = DbWerte.KESSEL_WARTUNG_EINHEIT_PROZENT
            };
            prozent.WartungAusNutzungsdauertabelle();
            Assert.Equal(1.5, prozent.Wartungskosten, 9);

            var gepflegt = new HeizkesselStammCtrl
            {
                Wartungskosten = 2.25, Wartungskosten_Einheit = DbWerte.KESSEL_WARTUNG_EINHEIT_PROZENT
            };
            gepflegt.WartungAusNutzungsdauertabelle();
            Assert.Equal(2.25, gepflegt.Wartungskosten, 9);

            var jahresbetrag = new HeizkesselStammCtrl
            {
                Wartungskosten = 0.0, Wartungskosten_Einheit = DbWerte.KESSEL_WARTUNG_EINHEIT_JAHR
            };
            jahresbetrag.WartungAusNutzungsdauertabelle();
            Assert.Equal(0.0, jahresbetrag.Wartungskosten);
        }

        // =====================================================================
        //  Rechenweg, Herleitung, Formelmappe
        // =====================================================================

        /// <summary>
        /// <b>Die Tabelle rechnet nicht selbst</b> (Fassung E10/9, Anwenderentscheid ND‑Q4):
        /// Projekt 1018 wie ausgeliefert — die vier Zeilen „Instandhaltung …" ohne Satz und
        /// ohne Betrag rechnen wie vor S3 mit nichts und tragen keine Herkunft. Nachweis und
        /// Summenschleife tragen dieselbe Zahl (Probe E7).
        /// </summary>
        [Fact]
        public void Ohne_eigenen_Satz_rechnet_die_Position_wie_bisher_mit_nichts()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            NutzungsdauerCtrl.ProbeVergessen();

            Dictionary<int, KostenPositionNachweis> n = Nachweise();
            foreach (int id in new[] { B_BHKW, B_WAERMEZENTRALE, B_BAULICH, B_STROMEINSPEISUNG, B_PERSONAL })
            {
                Assert.Null(n[id].Einheitpreis);
                Assert.Equal(0.0, n[id].BetragJahr, 9);
                Assert.Null(n[id].SatzHerkunft);
            }

            WirtschaftlichkeitCtrl.BetriebsTopfe t =
                WirtschaftlichkeitCtrl.LiesBetriebskostenTopfe(PROJEKT, WirtschaftlichkeitSzenario.ERWARTET);
            Assert.Equal(n.Values.Sum(x => x.BetragJahr), t.Gesamt, 6);
        }

        /// <summary>
        /// Ein Satz, der IN DER ZEILE steht, rechnet — gleich woher. Ist er genau der der
        /// Tabelle (vorbelegt oder übernommen), nennen Nachweis, Herleitung und Formelmappe
        /// die Herkunft; ein eigener Satz rechnet ohne sie, ein Betrag bleibt ein Betrag, und
        /// führt die Tabelle keinen Satz mehr, rechnet der gepflegte weiter — ohne Herkunft.
        /// </summary>
        [Fact]
        public void Ein_gepflegter_Satz_rechnet_und_nennt_die_Tabelle_nur_wenn_er_ihr_entspricht()
        {
            using var _ = new Kulturvorrichtung();
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            NutzungsdauerCtrl.ProbeVergessen();
            double basis = BetriebskostenCtrl.InvestSummeFuer(PROJEKT, BHKW, ANLAGE).Value;
            Assert.True(basis > 0);

            // Der Satz der Tabelle, ausdrücklich in der Zeile.
            SetzeSatz(B_BHKW, 6.0);
            KostenPositionNachweis tabelle = Nachweise()[B_BHKW];
            Pruefe(tabelle, 6.0, basis);

            // Herleitung (Wort, Tabelle, Spalte „Herleitung" der Formelmappe).
            string herleitung = WirtschaftlichkeitZeilen.Herleitung(tabelle, System.Globalization.CultureInfo.CurrentCulture);
            Assert.EndsWith(" · Satz aus Nutzungsdauertabelle", herleitung);
            Assert.Contains("6,000", herleitung);

            // Formelmappe Stufe 3: Menge × Satz / 100 besteht die Gegenrechnung.
            Assert.Equal(tabelle.BetragJahr,
                BetriebskostenCtrl.Betrag(tabelle.Bemessung, 0.0, tabelle.Menge,
                                          tabelle.Einheitpreis, tabelle.IstErloes), 6);

            // Probe E7 auch mit Satz: Summenschleife und Nachweisliste gehen denselben Weg.
            Dictionary<int, KostenPositionNachweis> alle = Nachweise();
            Assert.Equal(alle.Values.Sum(x => x.BetragJahr),
                         WirtschaftlichkeitCtrl.LiesBetriebskostenTopfe(PROJEKT, WirtschaftlichkeitSzenario.ERWARTET).Gesamt, 6);

            // Ein eigener Satz rechnet — ohne Herkunft.
            SetzeSatz(B_BHKW, 4.0);
            KostenPositionNachweis eigen = Nachweise()[B_BHKW];
            Assert.Equal(basis * 0.04, eigen.BetragJahr, 6);
            Assert.Null(eigen.SatzHerkunft);

            // Ein Betrag bleibt ein Betrag.
            SetzeSatz(B_BHKW, null);
            DataRepository.ExecuteNonQuery("UPDATE Tab_ProjektWerte SET EingegebenerWert = 500 WHERE ID = " + B_BHKW);
            KostenPositionNachweis erfasst = Nachweise()[B_BHKW];
            Assert.Equal(500.0, erfasst.BetragJahr, 6);
            Assert.Null(erfasst.SatzHerkunft);

            // Führt die Tabelle keinen Satz mehr, rechnet der gepflegte weiter — ohne Herkunft.
            DataRepository.ExecuteNonQuery("UPDATE Tab_ProjektWerte SET EingegebenerWert = 0 WHERE ID = " + B_BHKW);
            SetzeSatz(B_BHKW, 6.0);
            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_Nutzungsdauer SET Instandsetzung_Prozent = NULL WHERE ID = " + ZEILE_BHKW);
            KostenPositionNachweis ohneTabelle = Nachweise()[B_BHKW];
            Assert.Equal(basis * 0.06, ohneTabelle.BetragJahr, 6);
            Assert.Null(ohneTabelle.SatzHerkunft);
        }

        // =====================================================================
        //  Hilfen
        // =====================================================================

        /// <summary>Ein Lesestand ohne Datenbank — die Zeilen, die die Fälle brauchen.</summary>
        private static NutzungsdauerSatztafel Probetafel()
        {
            return new NutzungsdauerSatztafel(new List<NutzungsdauerZeile>
            {
                new NutzungsdauerZeile { Id = 10, KomponentenId = 1, Technik = "Wärmepumpe", Positionsart = "Gerät",
                                         IstStandard = true, InstandsetzungProzent = 2.5 },
                new NutzungsdauerZeile { Id = 11, KomponentenId = 1, Technik = "Wärmepumpe",
                                         Positionsart = "Erdsonden / Erdkollektor", InstandsetzungProzent = 1.5 },
                new NutzungsdauerZeile { Id = ZEILE_BHKW, KomponentenId = BHKW, Technik = "Blockheizkraftwerk",
                                         Positionsart = "Modul", IstStandard = true, InstandsetzungProzent = 6.0 },
                new NutzungsdauerZeile { Id = ZEILE_BATTERIE, KomponentenId = 5, Technik = "Stromspeicher",
                                         Positionsart = "Batterie", IstStandard = true },
                new NutzungsdauerZeile { Id = 22, KomponentenId = 6, Technik = "Pufferspeicher", Positionsart = "Speicher",
                                         IstStandard = true, WartungProzent = 0.5 },
                new NutzungsdauerZeile { Id = ZEILE_WAERMEZENTRALE, KomponentenId = 8, Technik = "Wärmezentrale",
                                         Positionsart = "Rohrleitungen", IstStandard = true, InstandsetzungProzent = 2.0 },
            });
        }

        private static void Pruefe(KostenPositionNachweis n, double satz, double basis)
        {
            Assert.Equal(satz, n.Einheitpreis.Value, 9);
            Assert.Equal(basis, n.Menge.Value, 6);
            Assert.Equal(basis * satz / 100.0, n.BetragJahr, 6);
            Assert.Equal(NutzungsdauerSatzCtrl.HERKUNFT_TABELLE, n.SatzHerkunft);
        }

        private static Dictionary<int, KostenPositionNachweis> Nachweise()
        {
            var d = new Dictionary<int, KostenPositionNachweis>();
            foreach (KostenPositionNachweis n in WirtschaftlichkeitCtrl.LiesBetriebskostenPositionen(
                         PROJEKT, WirtschaftlichkeitSzenario.ERWARTET))
                d[n.Id] = n;
            return d;
        }

        private static void SetzeSatz(int id, double? satz)
        {
            var p = new DbParam("@e", DbParamTyp.Double);
            p.Wert = satz.HasValue ? (object)satz.Value : DBNull.Value;
            DataRepository.ExecuteNonQuery("UPDATE Tab_ProjektWerte SET Einheitpreis = ? WHERE ID = " + id, p);
        }

        private static double? Instandsetzung(int zeile)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT Instandsetzung_Prozent FROM Tab_Nutzungsdauer WHERE ID = " + zeile);
            return o == null || o == DBNull.Value ? (double?)null : Convert.ToDouble(o);
        }

        private static double? SatzDerZeile(int id)
        {
            object o = DataRepository.ExecuteScalar("SELECT Einheitpreis FROM Tab_ProjektWerte WHERE ID = " + id);
            return o == null || o == DBNull.Value ? (double?)null : Convert.ToDouble(o);
        }

        private static double? SatzDerPosition(int projekt, string bezeichnung)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT w.Einheitpreis FROM Tab_ProjektWerte AS w INNER JOIN Tab_Kostenfaktor AS k " +
                "ON k.StammID = w.StammID WHERE w.ProjektID = ? AND w.KategorieID = ? AND k.Bezeichnung = ?",
                new DbParam("@p", projekt),
                new DbParam("@k", DbWerte.KOSTEN_KATEGORIE_BETRIEB),
                new DbParam("@b", bezeichnung));
            return o == null || o == DBNull.Value ? (double?)null : Convert.ToDouble(o);
        }

        private static int Zahl(string sql)
        {
            object o = DataRepository.ExecuteScalar(sql);
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o);
        }

        private static double? RepoSatz(SqliteConnection verbindung, int zeile)
        {
            using SqliteCommand befehl = verbindung.CreateCommand();
            befehl.CommandText = "SELECT Instandsetzung_Prozent FROM Tab_Nutzungsdauer WHERE ID = " + zeile;
            object o = befehl.ExecuteScalar();
            return o == null || o == DBNull.Value ? (double?)null : Convert.ToDouble(o);
        }

        /// <summary>Die Repowurzel über den Pfad dieser Quelldatei, sonst aufwärts bis zu
        /// <c>WP-Plan.sln</c>; <c>null</c>, wenn es keine gibt.</summary>
        private static string Repowurzel(
            [System.Runtime.CompilerServices.CallerFilePath] string eigeneDatei = null)
        {
            if (!string.IsNullOrEmpty(eigeneDatei))
            {
                string ordner = Path.GetDirectoryName(eigeneDatei);
                string kandidat = ordner == null ? null : Path.GetDirectoryName(ordner);
                if (kandidat != null && File.Exists(Path.Combine(kandidat, "WP-Plan.sln"))) return kandidat;
            }
            for (DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory); d != null; d = d.Parent)
                if (File.Exists(Path.Combine(d.FullName, "WP-Plan.sln"))) return d.FullName;
            return null;
        }
    }
}
