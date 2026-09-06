using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Nachweis der Stufe S1 des Wechselrichterkatalogs</b> (Anwenderentscheid
    /// <b>W6‑E‑2</b> vom 06.09.2026, <c>Konzept_Wechselrichter_EPOS-Plan.md</c>).
    ///
    /// <para><b>Was hier geprüft wird und was nicht.</b> Geprüft wird alles, was ohne
    /// Oberfläche entscheidbar ist: das Schema (Spalte für Spalte gleich zwischen
    /// Katalog und Projektkopie), die zwei Controller samt <c>CopyFromStamm</c>, die
    /// Dublettenprüfung über die Registry, die Plausibilität, die
    /// Sandia→Stützstellen-Umrechnung samt ihrem Prüfwert und der Leser der
    /// Importprobe. Nicht geprüft wird ein Rechenergebnis — <b>S1 hat keines</b>: Kein
    /// Rechenweg liest die zwei Tabellen, und der Referenzlauf bleibt byte-gleich.</para>
    ///
    /// <para><b>Eine Arbeitskopie je Klasse</b> (Regel seit iU9‑W11a); fehlt die Datei,
    /// schweigen die Fälle. Die Klasse trägt <c>[Collection("Testdatenbank")]</c>, weil
    /// <c>DataRepository.PfadUeberschreibung</c> statisch ist.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class WechselrichterKatalogTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;

        public WechselrichterKatalogTests(TestDatenbank db) { _db = db; }

        // =================================================================================
        // 1 — Das Schema (Migrationsschritt 65)
        // =================================================================================

        /// <summary>
        /// Beide Tabellen stehen, und sie sind Spalte für Spalte gleich: Der Katalog
        /// führt zusätzlich <c>ReadOnly</c>, die Projektkopie zusätzlich
        /// <c>ID_Projekt</c>, sonst nichts.
        /// </summary>
        /// <remarks>
        /// <b>Das ist die eigentliche Zusage von Schritt 65</b> (Konzept 3, Hausregeln):
        /// „Katalog und Projektkopie im selben Schritt, Spalte für Spalte gleich — eine
        /// Spalte nur auf einer Seite ist beim <c>CopyFromStamm</c> sofort ein
        /// Datenverlust."
        /// </remarks>
        [Fact]
        public void Katalog_und_Projektkopie_sind_spaltengleich()
        {
            if (!_db.Vorhanden) return;

            List<string> stamm = Spalten(WechselrichterStammCtrl.TABLE);
            List<string> projekt = Spalten(WechselrichterCtrl.TABLE);

            Assert.Equal(34, stamm.Count);
            Assert.Equal(34, projekt.Count);

            Assert.Equal(new[] { "ReadOnly" }, stamm.Except(projekt, Vergleich).ToArray());
            Assert.Equal(new[] { "ID_Projekt" }, projekt.Except(stamm, Vergleich).ToArray());
        }

        /// <summary>
        /// Jede Fachspalte des Schemas steht in
        /// <see cref="WechselrichterSchema.Fachspalten"/> — an dieser Liste hängen
        /// <c>Insert</c>, <c>Update</c> und <c>CopyFromStamm</c>.
        /// </summary>
        [Fact]
        public void Die_Fachspaltenliste_deckt_das_Schema_ab()
        {
            if (!_db.Vorhanden) return;

            var verwaltung = new[] { "ID", "ID_Projekt", "Bezeichner", "Firma", "Beschreibung", "ReadOnly" };
            List<string> fach = Spalten(WechselrichterStammCtrl.TABLE)
                                .Except(verwaltung, Vergleich).ToList();

            Assert.Equal(fach, WechselrichterSchema.Fachspalten.ToList());
        }

        /// <summary>
        /// <b>Die Migration ist idempotent</b>: Beide Anweisungen tragen
        /// <c>IF NOT EXISTS</c>, ein zweiter Lauf legt nichts an und ändert nichts. Der
        /// Fall setzt sie ein zweites Mal ab und prüft, dass Spaltenzahl und Inhalt
        /// gleich bleiben.
        /// </summary>
        [Fact]
        public void Der_Migrationsschritt_65_ist_idempotent()
        {
            if (!_db.Vorhanden) return;

            var ctrl = new WechselrichterStammCtrl { m_szName = "Idempotenzprobe", m_P_AC_Nenn = 5.0 };
            Assert.True(ctrl.Insert());

            foreach (KeyValuePair<string, string> a in WechselrichterSchema.Anweisungen)
                Assert.True(DataRepository.ExecuteSQL(a.Value), a.Key + ": Zweitlauf schlug fehl");

            Assert.Equal(34, Spalten(WechselrichterStammCtrl.TABLE).Count);
            Assert.Equal(34, Spalten(WechselrichterCtrl.TABLE).Count);

            var nachher = new WechselrichterStammCtrl();
            nachher.ReadSingle("Idempotenzprobe");
            Assert.Equal(1, nachher.rows);
            Assert.Equal(5.0, nachher.items[0].m_P_AC_Nenn);

            Assert.True(new WechselrichterStammCtrl().Delete("Idempotenzprobe"));
        }

        // =================================================================================
        // 2 — Der Katalogcontroller
        // =================================================================================

        /// <summary>
        /// Anlegen, Lesen, Ändern, Löschen — und NULL bleibt NULL. Beim Wechselrichter
        /// heißt NULL „keine Prüfung"; eine 0 hieße „Grenze null Volt".
        /// </summary>
        [Fact]
        public void Der_Katalogsatz_laesst_sich_anlegen_lesen_aendern_und_loeschen()
        {
            if (!_db.Vorhanden) return;

            var neu = Muster("CRUD-Probe");
            Assert.True(new WechselrichterStammCtrl().InsertFrom(neu));

            var gelesen = new WechselrichterStammCtrl();
            gelesen.ReadSingle("CRUD-Probe");
            Assert.Equal(1, gelesen.rows);

            WechselrichterModel m = gelesen.items[0];
            Assert.Equal(2.5, m.m_P_AC_Nenn);
            Assert.Equal(600.0, m.m_U_Dc_Max);
            Assert.Equal(1, m.m_Anzahl_Mppt);
            Assert.Equal(0.97, m.m_Eta100);
            Assert.Equal(DbWerte.WR_HERKUNFT_HAND, m.m_Herkunft);
            Assert.False(m.m_bReadOnly);

            // NULL bleibt NULL - nicht 0.
            Assert.Null(m.m_S_AC_Max);
            Assert.Null(m.m_U_Start);
            Assert.Null(m.m_Sandia_C3);

            var aendern = new WechselrichterStammCtrl();
            aendern.UebernimmVon(neu);
            aendern.m_P_AC_Nenn = 3.0;
            aendern.m_Kosten = 1234.0;
            Assert.True(aendern.Update("CRUD-Probe"));

            var wieder = new WechselrichterStammCtrl();
            wieder.ReadSingle("CRUD-Probe");
            Assert.Equal(3.0, wieder.items[0].m_P_AC_Nenn);
            Assert.Equal(1234.0, wieder.items[0].m_Kosten);

            Assert.True(new WechselrichterStammCtrl().Delete("CRUD-Probe"));
            Assert.False(new WechselrichterStammCtrl().Exists("CRUD-Probe"));
        }

        /// <summary>
        /// Der Herstellerfilter der Verwaltung (Konzept 6): <see
        /// cref="WechselrichterStammCtrl.Hersteller"/> nennt jede Firma einmal,
        /// <c>Filtern</c> engt darauf ein, und „Alle" hebt die Einengung auf.
        /// </summary>
        [Fact]
        public void Der_Herstellerfilter_engt_die_Liste_ein()
        {
            if (!_db.Vorhanden) return;

            // Der Katalog der Testdatenbank ist seit W6-O-7 nicht mehr leer (das
            // "Muster 2500TL" des Pruefprojekts 1045). Gezaehlt wird deshalb der
            // ZUWACHS, nicht der Bestand - die Aussage des Falls ist die Einengung.
            var ctrl = new WechselrichterStammCtrl();
            int vorher = ctrl.Filtern("Alle").Count;

            Anlegen("Filter A1", "Alpha AG");
            Anlegen("Filter A2", "Alpha AG");
            Anlegen("Filter B1", "Beta GmbH");

            Assert.Equal(vorher + 3, ctrl.Filtern("Alle").Count);
            Assert.Equal(2, ctrl.Filtern("Alpha AG").Count);
            Assert.Equal(1, ctrl.Filtern("Beta GmbH").Count);
            Assert.Empty(ctrl.Filtern("Gibt es nicht"));

            IReadOnlyList<string> firmen = WechselrichterStammCtrl.Hersteller();
            Assert.Contains("Alpha AG", firmen);
            Assert.Contains("Beta GmbH", firmen);
            Assert.Equal(firmen.Count, firmen.Distinct(StringComparer.Ordinal).Count());

            Aufraeumen("Filter A1", "Filter A2", "Filter B1");
        }

        /// <summary>
        /// <b>Der Auslieferungssatz ist gesperrt.</b> <c>ReadOnly = 1</c> verhindert
        /// Ändern und Löschen — dieselbe Regel wie in allen <c>_STAMM</c>-Katalogen.
        /// </summary>
        [Fact]
        public void Ein_schreibgeschuetzter_Satz_laesst_sich_weder_aendern_noch_loeschen()
        {
            if (!_db.Vorhanden) return;

            var ctrl = new WechselrichterStammCtrl();
            Assert.True(ctrl.InsertFrom(Muster("ReadOnly-Probe")));
            int id = ctrl.m_ID;

            DataRepository.ExecuteSQL(
                "UPDATE [" + WechselrichterStammCtrl.TABLE + "] SET ReadOnly = 1 WHERE ID = ?",
                new DbParam("@id", id));
            Assert.True(WechselrichterStammCtrl.IsReadOnlyById(id));

            // Die Sperre MELDET - der stille Dialogdienst schluckt die Meldung.
            var aendern = new WechselrichterStammCtrl();
            aendern.UebernimmVon(Muster("ReadOnly-Probe"));
            Assert.False(aendern.Update(id));
            Assert.False(new WechselrichterStammCtrl().Delete(id));

            DataRepository.ExecuteSQL(
                "DELETE FROM [" + WechselrichterStammCtrl.TABLE + "] WHERE ID = ?",
                new DbParam("@id", id));
        }

        /// <summary>
        /// <see cref="WechselrichterStammCtrl.SpeichernAus"/> — der EINE Schreibeinstieg
        /// der Verwaltung: leerer Name, Namensdublette beim Anlegen und ein
        /// Plausibilitätsfehler kommen als Ergebnis zurück, nicht als Dialog.
        /// </summary>
        [Fact]
        public void Der_Schreibeinstieg_meldet_seine_vier_Ausgaenge()
        {
            if (!_db.Vorhanden) return;

            Assert.False(WechselrichterStammCtrl.SpeichernAus(Muster(""), true, "").Ok);

            // Ohne AC-Nennleistung sperrt die Plausibilitaet.
            WechselrichterModel ohne = Muster("Einstieg-Probe");
            ohne.m_P_AC_Nenn = null;
            Assert.False(WechselrichterStammCtrl.SpeichernAus(ohne, true, "").Ok);

            Assert.True(WechselrichterStammCtrl.SpeichernAus(Muster("Einstieg-Probe"), true, "").Ok);
            Assert.False(WechselrichterStammCtrl.SpeichernAus(Muster("Einstieg-Probe"), true, "").Ok);

            WechselrichterStammCtrl.SpeicherErgebnis geaendert =
                WechselrichterStammCtrl.SpeichernAus(Muster("Einstieg-Probe"), false, "Einstieg-Probe");
            Assert.True(geaendert.Ok);
            Assert.Equal("Einstieg-Probe", geaendert.Name);

            Assert.True(WechselrichterStammCtrl.Loeschen("Einstieg-Probe").Ok);
        }

        /// <summary>
        /// <c>UpdateImport</c> schreibt GENAU die Importfelder: <c>Kosten</c>,
        /// <c>Bezeichner</c> und <c>Beschreibung</c> bleiben stehen
        /// (Dublettenkonzept 4.2).
        /// </summary>
        [Fact]
        public void Das_Import_Ueberschreiben_laesst_die_Anwenderfelder_stehen()
        {
            if (!_db.Vorhanden) return;

            var ctrl = new WechselrichterStammCtrl();
            WechselrichterModel satz = Muster("Import-Probe");
            satz.m_Kosten = 999.0;
            satz.m_szBeschreibung = "vom Anwender";
            Assert.True(ctrl.InsertFrom(satz));
            int id = ctrl.m_ID;

            var neu = new WechselrichterStammCtrl();
            neu.UebernimmVon(Muster("Import-Probe"));
            neu.m_P_AC_Nenn = 7.5;
            neu.m_Kosten = null;
            neu.m_szBeschreibung = "aus dem Import";
            Assert.True(neu.UpdateImport(id));

            var gelesen = new WechselrichterStammCtrl();
            gelesen.ReadSingle("Import-Probe");
            Assert.Equal(7.5, gelesen.items[0].m_P_AC_Nenn);
            Assert.Equal(999.0, gelesen.items[0].m_Kosten);
            Assert.Equal("vom Anwender", gelesen.items[0].m_szBeschreibung);

            Aufraeumen("Import-Probe");
        }

        /// <summary>
        /// <c>Kosten</c> steht NICHT in den Importspalten — sie ist ein Anwenderfeld,
        /// genau wie <c>Modulkosten</c> beim PV-Modul (Konzept 5.4).
        /// </summary>
        [Fact]
        public void Die_Importspalten_lassen_die_Kosten_aus()
        {
            string[] importspalten = WechselrichterStammCtrl.Importspalten();

            Assert.DoesNotContain(WechselrichterSchema.SPALTE_KOSTEN, importspalten);
            Assert.Equal(WechselrichterSchema.Fachspalten.Length - 1, importspalten.Length);
        }

        // =================================================================================
        // 3 — Die Projektkopie
        // =================================================================================

        /// <summary>
        /// <b><c>CopyFromStamm</c> kopiert JEDE Fachspalte</b> — das ist die Zusage aus
        /// Konzept 3.2. <c>ReadOnly</c> geht nicht mit (die Spalte gibt es dort nicht),
        /// ein zweiter Aufruf legt keine zweite Kopie an.
        /// </summary>
        [Fact]
        public void Die_Projektkopie_uebernimmt_jede_Fachspalte()
        {
            if (!_db.Vorhanden) return;

            const int idProjekt = 1030;

            var ctrl = new WechselrichterStammCtrl();
            Assert.True(ctrl.InsertFrom(Muster("Kopie-Probe")));
            int stammId = ctrl.m_ID;

            var projekt = new WechselrichterCtrl();
            int kopieId = projekt.CopyFromStamm(stammId, idProjekt);
            Assert.True(kopieId > 0);

            // Zweiter Aufruf: dieselbe Zeile, keine zweite Kopie.
            Assert.Equal(kopieId, projekt.CopyFromStamm(stammId, idProjekt));

            WechselrichterModel kopie = projekt.ReadSingle(kopieId);
            Assert.NotNull(kopie);
            Assert.Equal(idProjekt, kopie.m_ID_Projekt);

            var quelle = new WechselrichterStammCtrl();
            quelle.ReadSingle("Kopie-Probe");
            WechselrichterModel original = quelle.items[0];

            Assert.Equal(original.m_szName, kopie.m_szName);
            Assert.Equal(original.m_szFirma, kopie.m_szFirma);
            Assert.Equal(original.m_P_AC_Nenn, kopie.m_P_AC_Nenn);
            Assert.Equal(original.m_U_Mpp_Min, kopie.m_U_Mpp_Min);
            Assert.Equal(original.m_U_Dc_Max, kopie.m_U_Dc_Max);
            Assert.Equal(original.m_Anzahl_Mppt, kopie.m_Anzahl_Mppt);
            Assert.Equal(original.m_Eta05, kopie.m_Eta05);
            Assert.Equal(original.m_Eta100, kopie.m_Eta100);
            Assert.Equal(original.m_Eta_Euro, kopie.m_Eta_Euro);
            Assert.Equal(original.m_Kosten, kopie.m_Kosten);
            Assert.Equal(original.m_Sandia_Pdco, kopie.m_Sandia_Pdco);
            Assert.Equal(original.m_Herkunft, kopie.m_Herkunft);

            // Und die NULL-Werte sind auch in der Kopie NULL geblieben.
            Assert.Null(kopie.m_S_AC_Max);
            Assert.Null(kopie.m_U_Start);

            Assert.True(projekt.DeleteFromProjekt("Kopie-Probe", idProjekt));
            Aufraeumen("Kopie-Probe");
        }

        /// <summary>Ein unbekannter Katalogsatz liefert <c>-1</c> und legt nichts an.</summary>
        [Fact]
        public void Die_Projektkopie_meldet_einen_fehlenden_Katalogsatz()
        {
            if (!_db.Vorhanden) return;
            Assert.Equal(-1, new WechselrichterCtrl().CopyFromStamm(999999, 1030));
        }

        // =================================================================================
        // 4 — Die Dublettenprüfung über die Registry
        // =================================================================================

        /// <summary>
        /// Die Registry-Definition „WECHSELRICHTER" trägt die Dublettenprüfung: Ein
        /// zweiter Import desselben Geräts wird als <c>Identisch</c> gemeldet, ein
        /// gleichnamiger mit anderen Werten als <c>NameVorhanden</c>.
        /// </summary>
        [Fact]
        public void Die_Dublettenpruefung_erkennt_den_Zweitimport()
        {
            if (!_db.Vorhanden) return;

            KatalogDefinition katalog = KatalogRegistry.Finde("WECHSELRICHTER");
            Assert.NotNull(katalog);

            var ctrl = new WechselrichterStammCtrl();
            WechselrichterModel satz = Muster("Dubletten-Probe");
            Assert.True(ctrl.InsertFrom(satz));

            Assert.Equal(ImportBefund.Identisch, Befund(katalog, satz, "Dubletten-Probe"));

            WechselrichterModel anders = Muster("Dubletten-Probe");
            anders.m_P_AC_Nenn = 4.0;
            Assert.Equal(ImportBefund.NameVorhanden, Befund(katalog, anders, "Dubletten-Probe"));

            // Ein anderer NAME bei gleichem Inhalt ist "InhaltsGleich" - der Fall, den
            // der Konfliktdialog als "gewollte Variante trotzdem anlegen" anbietet.
            Assert.Equal(ImportBefund.InhaltsGleich,
                         Befund(katalog, Muster("Anderer Name"), "Anderer Name"));

            // Wirklich neu ist erst, was weder Name noch Inhalt trifft.
            WechselrichterModel fremd = Muster("Gibt es noch nicht");
            fremd.m_P_AC_Nenn = 12.5;
            fremd.m_szFirma = "Fremdwerk";
            Assert.Equal(ImportBefund.Neu, Befund(katalog, fremd, "Gibt es noch nicht"));

            Aufraeumen("Dubletten-Probe");
        }

        /// <summary>
        /// <b>Die Kosten machen keine Dublette</b>: Zwei Sätze, die sich nur im Preis
        /// unterscheiden, sind derselbe Wechselrichter (<c>AusschlussSpalten</c>,
        /// Konzept 5.4).
        /// </summary>
        [Fact]
        public void Ein_anderer_Preis_macht_keinen_anderen_Wechselrichter()
        {
            if (!_db.Vorhanden) return;

            KatalogDefinition katalog = KatalogRegistry.Finde("WECHSELRICHTER");

            WechselrichterModel satz = Muster("Preis-Probe");
            satz.m_Kosten = 1000.0;
            Assert.True(new WechselrichterStammCtrl().InsertFrom(satz));

            WechselrichterModel teurer = Muster("Preis-Probe");
            teurer.m_Kosten = 2000.0;
            Assert.Equal(ImportBefund.Identisch, Befund(katalog, teurer, "Preis-Probe"));

            Aufraeumen("Preis-Probe");
        }

        // =================================================================================
        // 5 — Sandia → Stützstellen
        // =================================================================================

        /// <summary>
        /// <b>Der Prüfwert aus Konzept 3.3.3:</b> Bei Nennlast gilt
        /// <c>η100 = Paco/Pdco</c> EXAKT. Die Zahlen sind das erste Gerät der echten
        /// CEC-Liste (ABB PVI-30-OUTD-S-US-A {208V}, Stand 06.09.2026).
        /// </summary>
        [Fact]
        public void Die_Stuetzstelle_bei_Nennlast_ist_Paco_durch_Pdco()
        {
            double?[] etas = WechselrichterKennlinie.AusSandia(3000, 3142.3, 18.1674, -8.03947e-06);

            Assert.All(etas, e => Assert.True(e.HasValue));
            Assert.Equal(3000.0 / 3142.3, etas[5].Value, 12);
        }

        /// <summary>
        /// Die Kennlinie steigt vom Teillastbereich bis 50 % und liegt überall
        /// in (0; 1] — der physikalische Verlauf eines Wechselrichters.
        /// </summary>
        [Fact]
        public void Die_Kennlinie_steigt_bis_zur_halben_Last()
        {
            double?[] etas = WechselrichterKennlinie.AusSandia(3000, 3142.3, 18.1674, -8.03947e-06);

            for (int i = 0; i < etas.Length; i++)
            {
                Assert.True(etas[i].Value > 0.0 && etas[i].Value <= 1.0,
                            "Stuetzstelle " + i + " liegt ausserhalb (0; 1]");
            }

            for (int i = 1; i <= 4; i++)
                Assert.True(etas[i].Value > etas[i - 1].Value,
                            "Die Kennlinie faellt zwischen Stuetzstelle " + (i - 1) + " und " + i);
        }

        /// <summary>
        /// Unbrauchbare Eingangswerte ergeben lauter <c>null</c> — eine unmögliche Zahl
        /// wird nicht geschrieben.
        /// </summary>
        [Theory]
        [InlineData(0.0, 3142.3, 18.0, -8.0e-06)]     // Paco = 0
        [InlineData(3000.0, 10.0, 18.0, -8.0e-06)]    // Pdco < Pso
        public void Unbrauchbare_Sandia_Werte_ergeben_keine_Kennlinie(double paco, double pdco,
                                                                      double pso, double c0)
        {
            Assert.All(WechselrichterKennlinie.AusSandia(paco, pdco, pso, c0), e => Assert.Null(e));
        }

        /// <summary>
        /// Der lineare Fall (<c>C0 = 0</c>) wird getrennt gerechnet, statt durch null zu
        /// teilen — und liefert denselben Prüfwert bei Nennlast.
        /// </summary>
        [Fact]
        public void Der_lineare_Fall_ohne_C0_traegt_denselben_Pruefwert()
        {
            double?[] etas = WechselrichterKennlinie.AusSandia(3000, 3142.3, 18.1674, 0.0);

            Assert.All(etas, e => Assert.True(e.HasValue));
            Assert.Equal(3000.0 / 3142.3, etas[5].Value, 12);
        }

        /// <summary>
        /// Der europäische Wirkungsgrad ist die gewichtete Summe (Konzept 3.3.1); die
        /// Gewichte summieren sich auf 1, und eine FEHLENDE Stützstelle liefert
        /// <c>null</c> statt einer erfundenen Zahl.
        /// </summary>
        [Fact]
        public void Der_Euro_Wirkungsgrad_wichtet_die_sechs_Stuetzstellen()
        {
            Assert.Equal(1.0, WechselrichterKennlinie.EURO_GEWICHTE.Sum(), 12);
            Assert.Equal(new[] { 0.05, 0.10, 0.20, 0.30, 0.50, 1.00 },
                         WechselrichterKennlinie.STUETZSTELLEN);

            // Eine flache Kennlinie mit 0,96 ueberall ergibt genau 0,96.
            var flach = new double?[] { 0.96, 0.96, 0.96, 0.96, 0.96, 0.96 };
            Assert.Equal(0.96, WechselrichterKennlinie.EuroWirkungsgrad(flach).Value, 12);

            // Das Beispiel aus Anhang A des Konzepts.
            var beispiel = new double?[] { 0.900, 0.940, 0.962, 0.970, 0.975, 0.970 };
            Assert.Equal(0.03 * 0.900 + 0.06 * 0.940 + 0.13 * 0.962
                         + 0.10 * 0.970 + 0.48 * 0.975 + 0.20 * 0.970,
                         WechselrichterKennlinie.EuroWirkungsgrad(beispiel).Value, 12);

            Assert.Null(WechselrichterKennlinie.EuroWirkungsgrad(
                new double?[] { 0.9, null, 0.96, 0.97, 0.975, 0.97 }));
            Assert.Null(WechselrichterKennlinie.EuroWirkungsgrad(null));
        }

        // =================================================================================
        // 6 — Die Plausibilität
        // =================================================================================

        /// <summary>Ein vollständiger, sinnvoller Satz meldet nichts.</summary>
        [Fact]
        public void Ein_plausibler_Katalogsatz_meldet_nichts()
        {
            WechselrichterPlausibilitaet.Befund b = WechselrichterPlausibilitaet.Pruefe(Muster("Sauber"));

            Assert.True(b.Ok);
            Assert.Empty(b.Warnungen);
            Assert.Equal("", WechselrichterPlausibilitaet.Meldung(b));
        }

        /// <summary>
        /// Die harten Verstöße sperren das Schreiben — jeder für sich.
        /// </summary>
        [Theory]
        [InlineData("kein Satz")]
        [InlineData("P_AC fehlt")]
        [InlineData("Paco ueber Pdco")]
        [InlineData("MPP-Fenster leer")]
        [InlineData("MPP ueber U_dc")]
        [InlineData("MPPT null")]
        [InlineData("Eta ueber eins")]
        [InlineData("Kosten negativ")]
        public void Ein_harter_Verstoss_sperrt_das_Schreiben(string fall)
        {
            WechselrichterModel m = Muster("Fehlerprobe");

            switch (fall)
            {
                case "kein Satz": m = null; break;
                case "P_AC fehlt": m.m_P_AC_Nenn = null; break;
                case "Paco ueber Pdco": m.m_Sandia_Pdco = 100.0; break;
                case "MPP-Fenster leer": m.m_U_Mpp_Min = 500.0; m.m_U_Mpp_Max = 100.0; break;
                case "MPP ueber U_dc": m.m_U_Mpp_Max = 900.0; break;
                case "MPPT null": m.m_Anzahl_Mppt = 0; break;
                case "Eta ueber eins": m.m_Eta50 = 1.5; break;
                case "Kosten negativ": m.m_Kosten = -1.0; break;
            }

            WechselrichterPlausibilitaet.Befund b = WechselrichterPlausibilitaet.Pruefe(m);
            Assert.False(b.Ok, fall + " haette ein Fehler sein muessen");
            Assert.NotEmpty(WechselrichterPlausibilitaet.Meldung(b));
        }

        /// <summary>
        /// <b>Ein fehlender Wert ist kein Fehler.</b> Bis auf <c>P_AC_Nenn</c> darf jede
        /// Spalte NULL sein — sie schaltet dann ihre Prüfung ab (Konzept 3.1).
        /// </summary>
        [Fact]
        public void Ein_Satz_mit_lauter_NULL_ausser_der_Nennleistung_ist_in_Ordnung()
        {
            var m = new WechselrichterModel { m_szName = "Nur Nennleistung", m_P_AC_Nenn = 2.5 };

            WechselrichterPlausibilitaet.Befund b = WechselrichterPlausibilitaet.Pruefe(m);
            Assert.True(b.Ok);
            Assert.Empty(b.Warnungen);
        }

        /// <summary>
        /// <b>Die Kennlinie darf nach 50 % wieder fallen</b> — genau das tut jedes
        /// Datenblatt (Anhang A: 0,975 bei 50 %, 0,970 bei 100 %). Wer über die ganze
        /// Kurve Monotonie forderte, meldete jedes echte Gerät.
        /// </summary>
        [Fact]
        public void Der_Abfall_hinter_der_halben_Last_ist_keine_Warnung()
        {
            WechselrichterModel m = Muster("Anhang A");
            m.m_Eta05 = 0.900; m.m_Eta10 = 0.940; m.m_Eta20 = 0.962;
            m.m_Eta30 = 0.970; m.m_Eta50 = 0.975; m.m_Eta100 = 0.970;
            m.m_Eta_Euro = 0.968;

            WechselrichterPlausibilitaet.Befund b = WechselrichterPlausibilitaet.Pruefe(m);
            Assert.True(b.Ok);
            Assert.Empty(b.Warnungen);

            // Gegenprobe: ein Abfall IM Teillastast wird gemeldet - als Warnung, nicht
            // als Fehler.
            m.m_Eta30 = 0.900;
            WechselrichterPlausibilitaet.Befund fallend = WechselrichterPlausibilitaet.Pruefe(m);
            Assert.True(fallend.Ok);
            Assert.NotEmpty(fallend.Warnungen);
        }

        /// <summary>
        /// <b>Jedes Gerät der echten CEC-Liste ist plausibel.</b> Der Fall fährt die
        /// Importprobe durch die Umrechnung und die Prüfung — wäre eine der beiden
        /// falsch, meldete er es an zwanzig echten Datenblättern statt an einem
        /// erfundenen.
        /// </summary>
        [Fact]
        public void Jedes_Geraet_der_Importprobe_ist_plausibel()
        {
            var dienst = new CecWechselrichterDienst();
            Assert.True(dienst.AusDatei(Probe()).Erfolg);

            foreach (CecWechselrichter g in dienst.AlleGeraete)
            {
                WechselrichterPlausibilitaet.Befund b =
                    WechselrichterPlausibilitaet.Pruefe(g.NachModell());
                Assert.True(b.Ok, g.Name + ": " + WechselrichterPlausibilitaet.Meldung(b));
            }
        }

        // =================================================================================
        // 7 — Der Leser der Importprobe
        // =================================================================================

        /// <summary>
        /// Die Probe: 21 Datenzeilen (20 Geräte, dazu EIN wortgleiches Duplikat des
        /// ersten). Kopf-, Einheiten- und <c>[0]</c>-Zeile werden übersprungen.
        /// </summary>
        [Fact]
        public void Die_Importprobe_liest_einundzwanzig_Geraete()
        {
            var dienst = new CecWechselrichterDienst();
            (bool Erfolg, CecFortschritt Meldung) r = dienst.AusDatei(Probe());

            Assert.True(r.Erfolg);
            Assert.Equal("CEC_MSG_GELADEN", r.Meldung.Schluessel);
            Assert.Equal("21", r.Meldung.Werte[0]);
            Assert.Equal(21, dienst.AlleGeraete.Count);

            // Das Duplikat traegt denselben Namen wie der erste Satz - der Fall, den
            // die Dublettenpruefung beim Uebernehmen meldet.
            Assert.Equal(dienst.AlleGeraete[0].Name, dienst.AlleGeraete[20].Name);
            Assert.Equal(20, dienst.AlleGeraete.Select(g => g.Name).Distinct().Count());
        }

        /// <summary>
        /// Die Feldzuordnung aus Konzept 5.1, am ersten Satz der Probe nachgerechnet:
        /// Hersteller aus dem Namen, <c>Paco</c> durch 1000 nach kW, MPP-Fenster und
        /// Spannungsgrenze unverändert, Herkunft <c>CEC</c>.
        /// </summary>
        [Fact]
        public void Die_Feldzuordnung_des_Imports_stimmt()
        {
            var dienst = new CecWechselrichterDienst();
            Assert.True(dienst.AusDatei(Probe()).Erfolg);

            CecWechselrichter g = dienst.AlleGeraete
                .First(x => x.Name.StartsWith("ABB:", StringComparison.Ordinal));

            Assert.Equal("ABB", g.Hersteller);
            Assert.Equal(3000.0, g.Paco);
            Assert.Equal(3142.3, g.Pdco);

            WechselrichterModel m = g.NachModell();
            Assert.Equal(3.0, m.m_P_AC_Nenn.Value, 9);
            Assert.Equal(100.0, m.m_U_Mpp_Min.Value, 9);
            Assert.Equal(480.0, m.m_U_Mpp_Max.Value, 9);
            Assert.Equal(480.0, m.m_U_Dc_Max.Value, 9);
            Assert.Equal(DbWerte.WR_HERKUNFT_CEC, m.m_Herkunft);
            Assert.Equal(3000.0 / 3142.3, m.m_Eta100.Value, 12);

            // Was die Liste nicht fuehrt, bleibt NULL - und wird nicht geraten.
            Assert.Null(m.m_Anzahl_Mppt);
            Assert.Null(m.m_Straenge_Je_Mppt);
            Assert.Null(m.m_S_AC_Max);
            Assert.Null(m.m_P_DC_Max);
            Assert.Null(m.m_Kosten);
        }

        /// <summary>
        /// Eine Datei ohne die Pflichtspalten wird ABGELEHNT statt still mit Nullen
        /// gelesen — dieselbe Regel wie beim Modulimport.
        /// </summary>
        [Fact]
        public void Eine_fremde_Kopfzeile_wird_abgelehnt()
        {
            string pfad = Path.Combine(Path.GetTempPath(),
                                       "wr-kopfzeile-" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".csv");
            File.WriteAllText(pfad, "Name,Irgendwas\nUnits,\n[0],\nEin Geraet,1\n");

            try
            {
                var dienst = new CecWechselrichterDienst();
                (bool Erfolg, CecFortschritt Meldung) r = dienst.AusDatei(pfad);

                Assert.False(r.Erfolg);
                Assert.Equal("CEC_MSG_KOPFZEILE", r.Meldung.Schluessel);
            }
            finally
            {
                try { File.Delete(pfad); } catch { /* Aufraeumen darf nicht scheitern */ }
            }
        }

        /// <summary>Eine fehlende Datei meldet sich, statt zu werfen.</summary>
        [Fact]
        public void Eine_fehlende_Datei_meldet_sich()
        {
            (bool Erfolg, CecFortschritt Meldung) r =
                new CecWechselrichterDienst().AusDatei(Path.Combine(Path.GetTempPath(), "gibt-es-nicht.csv"));

            Assert.False(r.Erfolg);
            Assert.Equal("CEC_MSG_DATEI_FEHLT", r.Meldung.Schluessel);
        }

        // =================================================================================
        // Hilfen
        // =================================================================================

        private static readonly IEqualityComparer<string> Vergleich = StringComparer.OrdinalIgnoreCase;

        /// <summary>
        /// Der Prüfsatz — das Muster 2500TL aus Anhang A des Konzepts, ergänzt um die
        /// Sandia-Werte, damit auch der Import-Pfad etwas zu vergleichen hat.
        /// </summary>
        private static WechselrichterModel Muster(string name)
        {
            return new WechselrichterModel
            {
                m_szName = name,
                m_szFirma = "Musterwerk",
                m_szBeschreibung = "Pruefsatz",
                m_P_AC_Nenn = 2.5,
                m_U_Mpp_Min = 80.0,
                m_U_Mpp_Max = 500.0,
                m_U_Dc_Max = 600.0,
                m_I_Dc_Max = 12.0,
                m_Anzahl_Mppt = 1,
                m_Eta05 = 0.900,
                m_Eta10 = 0.940,
                m_Eta20 = 0.962,
                m_Eta30 = 0.970,
                m_Eta50 = 0.975,
                m_Eta100 = 0.970,
                m_Eta_Euro = 0.968,
                m_P_Standby = 5.0,
                m_P_Nacht = 0.5,
                m_Sandia_Pdco = 2580.0,
                m_Herkunft = DbWerte.WR_HERKUNFT_HAND
            };
        }

        private static void Anlegen(string name, string firma)
        {
            WechselrichterModel m = Muster(name);
            m.m_szFirma = firma;
            Assert.True(new WechselrichterStammCtrl().InsertFrom(m));
        }

        private static void Aufraeumen(params string[] namen)
        {
            foreach (string n in namen)
                DataRepository.ExecuteSQL(
                    "DELETE FROM [" + WechselrichterStammCtrl.TABLE + "] WHERE Bezeichner = ?",
                    new DbParam("@bez", n));
        }

        /// <summary>Der Befund der Dublettenprüfung für EINEN Kandidaten.</summary>
        private static ImportBefund Befund(KatalogDefinition katalog, WechselrichterModel m, string name)
        {
            var kandidat = new ImportKandidat { Name = name, Tag = null };
            foreach (string spalte in katalog.ImportSpalten)
                kandidat.Werte[spalte] = Wert(m, spalte);
            kandidat.Werte["Bezeichner"] = name;

            List<ImportPruefung> pruefungen = DublettenPruefung.PruefeKandidaten(
                katalog, new List<ImportKandidat> { kandidat });
            return pruefungen.Count > 0 ? pruefungen[0].Befund : ImportBefund.Neu;
        }

        /// <summary>Der Wert einer Spalte des Prüfsatzes — über den Umweg der Datenbankzeile.</summary>
        private static object Wert(WechselrichterModel m, string spalte)
        {
            switch (spalte)
            {
                case "Firma": return m.m_szFirma;
                case "P_AC_Nenn": return m.m_P_AC_Nenn;
                case "S_AC_Max": return m.m_S_AC_Max;
                case "P_DC_Max": return m.m_P_DC_Max;
                case "U_Mpp_Min": return m.m_U_Mpp_Min;
                case "U_Mpp_Max": return m.m_U_Mpp_Max;
                case "U_Dc_Max": return m.m_U_Dc_Max;
                case "U_Start": return m.m_U_Start;
                case "I_Dc_Max": return m.m_I_Dc_Max;
                case "Anzahl_Mppt": return m.m_Anzahl_Mppt;
                case "Straenge_Je_Mppt": return m.m_Straenge_Je_Mppt;
                case "Eta05": return m.m_Eta05;
                case "Eta10": return m.m_Eta10;
                case "Eta20": return m.m_Eta20;
                case "Eta30": return m.m_Eta30;
                case "Eta50": return m.m_Eta50;
                case "Eta100": return m.m_Eta100;
                case "Eta_Euro": return m.m_Eta_Euro;
                case "Eta_Max": return m.m_Eta_Max;
                case "P_Standby": return m.m_P_Standby;
                case "P_Nacht": return m.m_P_Nacht;
                case "Herkunft": return m.m_Herkunft;
            }
            return null;
        }

        /// <summary>Die Spalten einer Tabelle in Schemareihenfolge.</summary>
        private static List<string> Spalten(string tabelle)
        {
            return DataRepository.SpaltenVonTabelle(tabelle) ?? new List<string>();
        }

        /// <summary>
        /// Die Importprobe unter <c>Referenzlaeufe/Importproben</c> — dasselbe
        /// Aufwärtssuchen wie in <c>KatalogImportTests</c>.
        /// </summary>
        private static string Probe()
        {
            var d = new DirectoryInfo(AppContext.BaseDirectory);
            for (int i = 0; i < 8 && d != null; i++, d = d.Parent)
            {
                string kandidat = Path.Combine(d.FullName, "Referenzlaeufe", "Importproben",
                                               "cec_wechselrichter_21.csv");
                if (File.Exists(kandidat)) return kandidat;
            }

            Assert.Fail("Die Importprobe cec_wechselrichter_21.csv wurde nicht gefunden.");
            return null;
        }
    }
}
