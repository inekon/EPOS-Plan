using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Nachweis des Anwenderentscheids W14a‑E‑10‑Q7</b> vom 07.09.2026
    /// („Bekommt <c>Tab_Stromspeicher_STAMM</c> eine Spalte <c>Firma</c>? Ja, als
    /// eigener Schemaschritt mit Nachtrag aus dem Bezeichnerpräfix") — Stufe S2 des
    /// <c>Konzept_Katalogfilter_EPOS-Plan.md</c>, Migrationsschritt <b>68</b>.
    ///
    /// <para><b>Was hier geprüft wird.</b> Die DDL des Schrittes (zwei Spalten, Stamm
    /// UND Projektkopie), das DML (<see cref="StromspeicherFirmaNachtrag"/>: nur was
    /// ein Präfix hat, und idempotent), der RÜCKFALL der Anzeige auf das Präfix, der
    /// VORRANG des gepflegten Werts und dass der Import beides schreibt.</para>
    ///
    /// <para><b>Der Befund, der den Rückfall nötig macht:</b> Die fünf Sätze der
    /// Testdatenbank tragen KEINEN Doppelpunkt im Bezeichner („BYD B‑Box HVM 11.0",
    /// „BYD HVS+ 12.8", „VARTA element backup", „VARTA pulse neo",
    /// „Vaillant 10030745"). Der Nachtrag trägt dort also nichts nach — die Spalte ist
    /// am Tag ihrer Entstehung leer, und erst der nächste CEC‑Import füllt sie. Der
    /// Nachtrag selbst wird deshalb an einem SYNTHETISCHEN Satz belegt.</para>
    ///
    /// <para>Eine Arbeitskopie je Klasse (Regel seit iU9‑W11a); fehlt die Datei,
    /// schweigen die Fälle. <c>[Collection("Testdatenbank")]</c>, weil
    /// <c>DataRepository.PfadUeberschreibung</c> statisch ist.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class StromspeicherFirmaTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;

        public StromspeicherFirmaTests(TestDatenbank db) { _db = db; }

        /// <summary>Der Bezeichner des synthetischen Prüfsatzes — MIT Präfix.</summary>
        private const string SATZ_MIT_PRAEFIX = "Prüf AG: PS-68 Nachtrag";

        /// <summary>Der Bezeichner des synthetischen Prüfsatzes OHNE Präfix.</summary>
        private const string SATZ_OHNE_PRAEFIX = "PS-68 ohne Praefix";

        // =================================================================================
        // 1 — Die DDL des Schrittes
        // =================================================================================

        /// <summary>
        /// Der Schritt legt die Spalte in BEIDEN Tabellen an — so halten es die sechs
        /// anderen Gerätekataloge, und ohne die Kopie verlöre die Übernahme in das
        /// Projekt den Hersteller.
        /// </summary>
        [Fact]
        public void Schritt_68_nennt_Stammtabelle_und_Projektkopie()
        {
            SchemaSpalte[] spalten = SchemaKatalog.Schritt68_StromspeicherFirma;

            Assert.Equal(2, spalten.Length);
            Assert.Contains(spalten, s => s.Tabelle == SchemaKatalog.TAB_STROMSPEICHER_STAMM);
            Assert.Contains(spalten, s => s.Tabelle == SchemaKatalog.TAB_STROMSPEICHER);
            Assert.All(spalten, s => Assert.Equal(SchemaKatalog.SPALTE_SP_FIRMA, s.Name));
            Assert.All(spalten, s => Assert.Equal("TEXT(255)", s.TypDefinition));

            // Kein DDL-DEFAULT auf einem Fachwert (Hausregel).
            Assert.All(spalten, s => Assert.DoesNotContain("DEFAULT", s.TypDefinition,
                                                           StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Die Rückfallebene kennt beide Spalten: <c>StromspeicherStammCtrl.Insert</c>
        /// und <c>…Update</c> nennen <c>Firma</c> seit S2 namentlich, und
        /// <see cref="SchemaKatalog.Alle"/> ist der Weg, über den
        /// <c>StelleGeraetespaltenSicher</c> sie anlegt.
        /// </summary>
        [Fact]
        public void Die_Rueckfallebene_kennt_beide_Spalten()
        {
            var alle = SchemaKatalog.Alle.ToList();

            Assert.Contains(alle, s => s.Tabelle == SchemaKatalog.TAB_STROMSPEICHER_STAMM &&
                                       s.Name == SchemaKatalog.SPALTE_SP_FIRMA);
            Assert.Contains(alle, s => s.Tabelle == SchemaKatalog.TAB_STROMSPEICHER &&
                                       s.Name == SchemaKatalog.SPALTE_SP_FIRMA);
        }

        /// <summary>
        /// Der Zielstand steht auf 68. Er ist zugleich die Zusage des
        /// <c>.wpx</c>-Formats: Ein Paket auf Stand 67 wird beim Import abgewiesen.
        /// </summary>
        [Fact]
        public void Der_Zielstand_steht_auf_68()
        {
            Assert.Equal(68, SchemaStand.Zielversion);
        }

        // =================================================================================
        // 2 — Das DML: der Nachtrag aus dem Bezeichnerpräfix
        // =================================================================================

        /// <summary>
        /// Die Anweisung fasst <b>nur</b> die eine Spalte an, schränkt auf „leer" ein und
        /// verlangt ein Präfix (<c>instr &gt; 1</c>) — sie RÄT nicht.
        /// </summary>
        [Fact]
        public void Die_Anweisung_nennt_nur_die_eine_Spalte_und_verlangt_ein_Praefix()
        {
            string sql = StromspeicherFirmaNachtrag.Nachtrag();

            Assert.Contains("Tab_Stromspeicher_STAMM", sql, StringComparison.Ordinal);
            Assert.Contains("SET Firma =", sql, StringComparison.Ordinal);
            Assert.Contains("instr(Bezeichner, ':') > 1", sql, StringComparison.Ordinal);
            Assert.Contains("Firma IS NULL OR Firma = ''", sql, StringComparison.Ordinal);
            Assert.DoesNotContain("Bezeichner =", sql, StringComparison.Ordinal);
            Assert.DoesNotContain("Tab_Stromspeicher ", sql, StringComparison.Ordinal);
        }

        /// <summary>
        /// <b>Der Nachtrag an einem synthetischen Satz.</b> Er trägt ein, was im
        /// Bezeichner steht — und nur dort, wo eines steht: Der Satz OHNE Doppelpunkt
        /// bleibt leer.
        /// </summary>
        [Fact]
        public void Der_Nachtrag_traegt_das_Praefix_ein_und_laesst_Saetze_ohne_Praefix_leer()
        {
            if (!_db.Vorhanden) return;

            Aufraeumen();
            Anlegen(SATZ_MIT_PRAEFIX);
            Anlegen(SATZ_OHNE_PRAEFIX);

            Assert.Equal(1, Zaehlung());

            DataRepository.ExecuteNonQuery(StromspeicherFirmaNachtrag.Nachtrag());

            Assert.Equal("Prüf AG", Firma(SATZ_MIT_PRAEFIX));
            Assert.True(string.IsNullOrEmpty(Firma(SATZ_OHNE_PRAEFIX)));
            Assert.Equal(0, Zaehlung());

            Aufraeumen();
        }

        /// <summary>
        /// <b>Idempotent.</b> Das <c>UPDATE</c> trägt seine Bedingung selbst; ein
        /// zweiter Lauf findet nichts mehr und ändert nichts — auch dann nicht, wenn
        /// jemand den gepflegten Wert inzwischen geändert hat.
        /// </summary>
        [Fact]
        public void Der_Nachtrag_ist_idempotent()
        {
            if (!_db.Vorhanden) return;

            Aufraeumen();
            Anlegen(SATZ_MIT_PRAEFIX);

            DataRepository.ExecuteNonQuery(StromspeicherFirmaNachtrag.Nachtrag());
            Assert.Equal("Prüf AG", Firma(SATZ_MIT_PRAEFIX));

            // Der Anwender bessert den Hersteller nach - der zweite Lauf darf ihn NICHT
            // wieder auf das Praefix zuruecksetzen.
            DataRepository.ExecuteSQL(
                "UPDATE Tab_Stromspeicher_STAMM SET Firma = ? WHERE Bezeichner = ?",
                new DbParam("@f", "Prüf Aktiengesellschaft"),
                new DbParam("@b", SATZ_MIT_PRAEFIX));

            DataRepository.ExecuteNonQuery(StromspeicherFirmaNachtrag.Nachtrag());

            Assert.Equal("Prüf Aktiengesellschaft", Firma(SATZ_MIT_PRAEFIX));
            Assert.Equal(0, Zaehlung());

            Aufraeumen();
        }

        /// <summary>
        /// <b>Der Befund der Testdatenbank:</b> fünf Sätze, KEINER mit Präfix. Genau
        /// deshalb behält die Anzeige ihren Rückfall — die Spalte ist am Tag ihrer
        /// Entstehung leer.
        /// </summary>
        [Fact]
        public void Die_fuenf_Altsaetze_der_Testdatenbank_tragen_kein_Praefix()
        {
            if (!_db.Vorhanden) return;

            Aufraeumen();

            object gesamt = DataRepository.ExecuteScalar(StromspeicherFirmaNachtrag.Gesamtzahl());
            Assert.Equal(5, Convert.ToInt32(gesamt));
            Assert.Equal(0, Zaehlung());
        }

        // =================================================================================
        // 3 — Der Rückfall der Anzeige und der Vorrang des gepflegten Werts
        // =================================================================================

        /// <summary>
        /// <c>StromspeicherStammCtrl.Hersteller</c> ist die eine Regel: gepflegter Wert
        /// zuerst, sonst das Präfix, sonst leer.
        /// </summary>
        [Theory]
        [InlineData("Sonnen GmbH", "BYD: HVS 12.8", "Sonnen GmbH")]   // Spalte schlaegt Praefix
        [InlineData("", "BYD: HVS 12.8", "BYD")]                      // Rueckfall auf das Praefix
        [InlineData(null, "BYD: HVS 12.8", "BYD")]                    // NULL wie leer
        [InlineData("  ", "BYD: HVS 12.8", "BYD")]                    // Leerraum zaehlt als leer
        [InlineData("", "VARTA pulse neo", "")]                       // kein Praefix, kein Hersteller
        [InlineData("Sonnen GmbH", "VARTA pulse neo", "Sonnen GmbH")]
        public void Der_gepflegte_Hersteller_schlaegt_das_Praefix(string firma, string bezeichner,
                                                                  string erwartet)
        {
            Assert.Equal(erwartet, StromspeicherStammCtrl.Hersteller(firma, bezeichner));
        }

        /// <summary>
        /// <b>Die Katalogliste zeigt den gepflegten Wert.</b> Der synthetische Satz
        /// bekommt eine Firma, die NICHT im Bezeichner steht — steht sie danach in der
        /// Spalte „Hersteller" der Katalogzeile, geht der Weg über die Spalte und nicht
        /// über das Präfix.
        /// </summary>
        [Fact]
        public void Die_Katalogzeile_zeigt_den_gepflegten_Hersteller()
        {
            if (!_db.Vorhanden) return;

            Aufraeumen();
            Anlegen(SATZ_OHNE_PRAEFIX);
            DataRepository.ExecuteSQL(
                "UPDATE Tab_Stromspeicher_STAMM SET Firma = ? WHERE Bezeichner = ?",
                new DbParam("@f", "Zelle & Kasten KG"),
                new DbParam("@b", SATZ_OHNE_PRAEFIX));

            Katalogfilterzeile zeile = StromspeicherStammCtrl.Katalogfilterzeilen()
                .FirstOrDefault(z => z.Bezeichner == SATZ_OHNE_PRAEFIX);

            Assert.NotNull(zeile);
            Assert.Equal("Zelle & Kasten KG", zeile!.Text(Katalogfilterprofil.SpHersteller));

            Aufraeumen();
        }

        /// <summary>
        /// <b>Der Rückfall in der Katalogliste.</b> Ohne gepflegten Wert steht das
        /// Präfix da; ohne beides der Halbgeviertstrich (W6‑E‑1: NULL ist etwas anderes
        /// als eine gemessene Null).
        /// </summary>
        [Fact]
        public void Die_Katalogzeile_faellt_auf_das_Praefix_zurueck()
        {
            if (!_db.Vorhanden) return;

            Aufraeumen();
            Anlegen(SATZ_MIT_PRAEFIX);
            Anlegen(SATZ_OHNE_PRAEFIX);

            var zeilen = StromspeicherStammCtrl.Katalogfilterzeilen();

            Assert.Equal("Prüf AG",
                zeilen.First(z => z.Bezeichner == SATZ_MIT_PRAEFIX)
                      .Text(Katalogfilterprofil.SpHersteller));
            Assert.Equal(ParameterVerwendung.LEER,
                zeilen.First(z => z.Bezeichner == SATZ_OHNE_PRAEFIX)
                      .Text(Katalogfilterprofil.SpHersteller));

            Aufraeumen();
        }

        // =================================================================================
        // 4 — Der Importweg und der Editor
        // =================================================================================

        /// <summary>
        /// <b>Beide Importwege schreiben die Firma ZUSÄTZLICH zum Präfix.</b> Der
        /// Bezeichner bleibt „Hersteller: Modell" — daran hängt die Wiedererkennung
        /// eines Satzes —, und die Spalte trägt den Hersteller noch einmal für sich.
        /// Wer den Namen im Konfliktdialog ändert, verliert ihn damit nicht.
        /// </summary>
        [Fact]
        public void Der_Import_schreibt_die_Firma_zusaetzlich_zum_Praefix()
        {
            var satz = new StromspeicherImportSatz
            {
                Hersteller = "  BYD  ",
                Modell = "HVS 12.8",
                Technologie = "Lithium Iron Phosphate",
                LeistungKw = 8.0,
                EnergieKwh = 12.8
            };

            Assert.Equal("BYD: HVS 12.8", satz.Bezeichner);

            StromspeicherModel m = satz.NachModell();
            Assert.Equal("BYD", m.m_szFirma);
            Assert.Equal("BYD: HVS 12.8", m.m_szBezeichner);

            // Auch mit umbenanntem Bezeichner bleibt der Hersteller stehen.
            StromspeicherModel u = satz.NachModell("Speicher Halle Nord");
            Assert.Equal("BYD", u.m_szFirma);
            Assert.Equal("Speicher Halle Nord", u.m_szBezeichner);
        }

        /// <summary>
        /// <b>Das Feld „Firma" steht im Editor.</b> Der Stromspeicher war der einzige
        /// Modulkatalog ohne — PV und Wechselrichter führen es seit jeher.
        /// </summary>
        [Fact]
        public void Der_Editor_fuehrt_ein_Feld_Firma()
        {
            ModulKatalogProfil p = ModulKatalogProfil.Finde(ModulKatalogArt.Stromspeicher);

            ModulKatalogFeld feld = p.Felder.FirstOrDefault(
                f => f.Schluessel == ModulKatalogProfil.FeldFirma);

            Assert.NotNull(feld);
            Assert.Equal(BrowserFeldArt.Text, feld!.Art);
            Assert.True(feld.LeerErlaubt);            // die fuenf Altsaetze fuehren keinen
            Assert.False(feld.Gesperrt);
            Assert.Equal(0, feld.Gruppe);             // Bestandsblock, nicht AP3

            // Es steht unmittelbar hinter dem Bezeichner - dieselbe Lage wie im
            // PV- und im Wechselrichterkatalog.
            int i = p.Felder.ToList().FindIndex(f => f.Schluessel == ModulKatalogProfil.FeldFirma);
            int b = p.Felder.ToList().FindIndex(f => f.Schluessel == ModulKatalogProfil.FeldBezeichner);
            Assert.Equal(b + 1, i);
        }

        /// <summary>
        /// Der Detailblock des Editors liefert die Firma — sonst stünde das neue Feld
        /// beim Durchklicken des Katalogs immer leer.
        /// </summary>
        [Fact]
        public void Der_Detailblock_liefert_die_Firma()
        {
            if (!_db.Vorhanden) return;

            Aufraeumen();
            Anlegen(SATZ_OHNE_PRAEFIX);
            DataRepository.ExecuteSQL(
                "UPDATE Tab_Stromspeicher_STAMM SET Firma = ? WHERE Bezeichner = ?",
                new DbParam("@f", "Zelle & Kasten KG"),
                new DbParam("@b", SATZ_OHNE_PRAEFIX));

            IReadOnlyDictionary<string, string> werte =
                StromspeicherStammCtrl.KatalogsatzAnzeige(SATZ_OHNE_PRAEFIX);

            Assert.NotNull(werte);
            Assert.Equal("Zelle & Kasten KG", werte[ModulKatalogProfil.FeldFirma]);

            Aufraeumen();
        }

        /// <summary>
        /// <b>Der Schreibweg des Editors trägt die Firma bis in die Zeile.</b>
        /// <c>SpeichernAus</c> ist der EINE Einstieg (W14a.0e); ohne die Spalte im
        /// <c>INSERT</c> ginge der Wert beim Anlegen still verloren.
        /// </summary>
        [Fact]
        public void Der_Schreibweg_legt_die_Firma_mit_an()
        {
            if (!_db.Vorhanden) return;

            Aufraeumen();

            var m = new StromspeicherModel
            {
                m_szBezeichner = SATZ_OHNE_PRAEFIX,
                m_szFirma = "Zelle & Kasten KG",
                m_szTyp = DbWerte.SP_TYP_LITHIUM_IONEN,
                m_Energie = 10,
                m_Leistung = 5
            };

            StromspeicherStammCtrl.SpeicherErgebnis e =
                StromspeicherStammCtrl.SpeichernAus(m, neu: true, schluessel: SATZ_OHNE_PRAEFIX);

            Assert.True(e.Ok, e.Meldung);
            Assert.Equal("Zelle & Kasten KG", Firma(SATZ_OHNE_PRAEFIX));

            // ... und der Aenderungsweg schreibt sie ebenfalls.
            m.m_szFirma = "Zelle AG";
            e = StromspeicherStammCtrl.SpeichernAus(m, neu: false, schluessel: SATZ_OHNE_PRAEFIX);

            Assert.True(e.Ok, e.Meldung);
            Assert.Equal("Zelle AG", Firma(SATZ_OHNE_PRAEFIX));

            Aufraeumen();
        }

        // =================================================================================
        // Hilfsmittel
        // =================================================================================

        private static int Zaehlung()
        {
            object o = DataRepository.ExecuteScalar(StromspeicherFirmaNachtrag.Zaehlung());
            return (o == null || o == DBNull.Value) ? -1 : Convert.ToInt32(o);
        }

        private static string Firma(string bezeichner)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT Firma FROM Tab_Stromspeicher_STAMM WHERE Bezeichner = ?",
                new DbParam("@b", bezeichner));
            return (o == null || o == DBNull.Value) ? "" : o.ToString();
        }

        /// <summary>Legt einen synthetischen Prüfsatz an — ohne Firma, wie vor dem Schritt.</summary>
        private static void Anlegen(string bezeichner)
        {
            int id = DataRepository.GetMaxID(StromspeicherStammCtrl.TABLE) + 1;
            DataRepository.ExecuteSQL(
                "INSERT INTO Tab_Stromspeicher_STAMM (ID, Bezeichner, Typ, Leistung, Energie, ReadOnly) " +
                "VALUES (?, ?, ?, ?, ?, ?)",
                new DbParam("@id", id),
                new DbParam("@b", bezeichner),
                new DbParam("@t", DbWerte.SP_TYP_LITHIUM_IONEN),
                new DbParam("@l", 5.0),
                new DbParam("@e", 10.0),
                new DbParam("@r", false));
        }

        /// <summary>
        /// Räumt die synthetischen Sätze ab. Sie werden je Fall neu angelegt, damit die
        /// Fälle einander nicht sehen — die Arbeitskopie lebt über die ganze Klasse.
        /// </summary>
        private static void Aufraeumen()
        {
            DataRepository.ExecuteSQL(
                "DELETE FROM Tab_Stromspeicher_STAMM WHERE Bezeichner = ? OR Bezeichner = ?",
                new DbParam("@a", SATZ_MIT_PRAEFIX),
                new DbParam("@b", SATZ_OHNE_PRAEFIX));
        }
    }
}
