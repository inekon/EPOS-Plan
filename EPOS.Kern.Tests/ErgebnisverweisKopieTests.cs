using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// DER KOPIERWEG NIMMT KEINE ERGEBNISSE MIT (Anwenderentscheid 23.09.2026: „Ergebnistabellen
    /// nicht mitkopieren") und Schemaschritt <b>106</b>.
    ///
    /// <para><b>Der Befund.</b> Der generische Kopierlauf (<see cref="ProjektDuplizierenCtrl"/>,
    /// auch der Weg jeder Variante über <see cref="VariantenCtrl"/>) kopierte die
    /// Ergebnistabellen mit: <c>Tab_Ergebnis</c> samt Detailtabellen und die gespeicherte
    /// Wirtschaftlichkeit. Die Kopie zeigte damit sofort Ergebnisse, die zum Quellprojekt
    /// gehören; <c>Tab_ErgebnisWirtschaftlichkeit.ID_Ergebnis</c> zeigte obendrein
    /// UNVERSETZT auf den Lauf der Quelle (in der Testdatenbank 21 Zeilen: 1028, 1029, 1040,
    /// 1041 auf Lauf 167 von 1026, 1043 und 1044 auf Lauf 206 von 1042).</para>
    ///
    /// <para><b>Die Regel.</b> Eine Ergebnistabelle (<c>ProjektDuplizierenCtrl.IstErgebnisTabelle</c>
    /// und jede Detailtabelle, die über ihren Fremdschlüssel an einer hängt) bleibt beim
    /// Kopieren aus: Die Kopie hat keinen Lauf und keine gespeicherte Wirtschaftlichkeit, wie
    /// ein neu angelegtes Projekt. Die Eingaben kommen vollständig mit. Der Projekttransfer
    /// (Export/Import) nimmt die Ergebnisse weiter mit. Schritt 106 bereinigt die fremden
    /// Verweise im Bestand.</para>
    ///
    /// <para><b>Eigene Arbeitskopie je Fall</b> — die Fälle schreiben.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class ErgebnisverweisKopieTests
    {
        /// <summary>Das Quellprojekt „Beispiel WP WG 1" mit eigenem Lauf 167 und drei Wirtschaftlichkeitszeilen.</summary>
        private const int QUELLE = 1026;
        private const string QUELLNAME = "Beispiel WP WG 1";
        private const int LAUF_QUELLE = 167;

        /// <summary>Die 21 Zeilen der Testdatenbank mit fremdem Verweis (vor Schritt 106).</summary>
        private static readonly int[] FREMD = { 16, 18, 20, 21, 23, 25, 189, 191, 193, 194, 196, 198,
                                                213, 214, 215, 216, 217, 218, 219, 220, 221 };

        // =================================================================================
        // 1 — Duplizieren und Variante
        // =================================================================================

        /// <summary>
        /// DUPLIZIEREN: Die Kopie trägt in keiner Ergebnistabelle eine Zeile — kein Lauf, keine
        /// Ergebnisdetails, keine gespeicherte Wirtschaftlichkeit, keine Sensitivität, keine
        /// Strommatrix —, aber in jeder Eingabetabelle genau die Zeilen der Quelle. Die Quelle
        /// bleibt in jeder Tabelle unverändert, und in keiner Ergebnistabelle kommt irgendwo
        /// eine Zeile hinzu.
        ///
        /// <para>„Wöhler - Test2" (1024) führt Lauf, Ergebnisdetails für BHKW, Kessel, Puffer,
        /// Wärmepumpe samt Modulen, drei Wirtschaftlichkeitszeilen und vier
        /// Sensitivitätszeilen; „BHKW Test München" (1018) dazu die Strommatrix.</para>
        /// </summary>
        [Theory]
        [InlineData("Wöhler - Test2")]
        [InlineData("BHKW Test München")]
        public void Die_Kopie_hat_keine_Ergebnisse_aber_alle_Eingaben(string quellname)
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var dup = new ProjektDuplizierenCtrl();
            int quelle = dup.GetProjektId(quellname);
            Assert.True(quelle > 0, quellname + " fehlt in der Testdatenbank.");

            List<ProjektDuplizierenCtrl.Spec> plan = dup.ErmittlePlan();
            Dictionary<string, int> quelleVorher = ZaehleJeTabelle(plan, quelle);
            Dictionary<string, int> gesamtVorher = GesamtJeErgebnistabelle(plan);
            QuelleTraegtLaufUndEingaben(quelleVorher);

            int kopie = dup.Duplizieren(quellname, quellname + " Ergebnisprobe");
            Assert.True(kopie > 0, "Duplizieren fehlgeschlagen.");

            KopieOhneErgebnisseMitEingaben(plan, quelle, kopie, quelleVorher, gesamtVorher,
                                           ausgenommen: null);
        }

        /// <summary>
        /// VARIANTE: derselbe Kopierlauf, dieselbe Regel. Einzig <c>Tab_Variante</c> weicht ab
        /// — die Verknüpfung trägt <see cref="VariantenCtrl"/> nach dem Kopieren selbst ein.
        /// </summary>
        [Fact]
        public void Die_Variante_hat_keine_Ergebnisse_aber_alle_Eingaben()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            const string STAMM = "BHKW Test München";
            var dup = new ProjektDuplizierenCtrl();
            int stamm = dup.GetProjektId(STAMM);
            Assert.True(stamm > 0);

            List<ProjektDuplizierenCtrl.Spec> plan = dup.ErmittlePlan();
            Dictionary<string, int> stammVorher = ZaehleJeTabelle(plan, stamm);
            Dictionary<string, int> gesamtVorher = GesamtJeErgebnistabelle(plan);
            QuelleTraegtLaufUndEingaben(stammVorher);

            int variante = new VariantenCtrl().AnlegenAusStamm(stamm, STAMM, "Ohne Lauf", out string fehler);
            Assert.True(variante > 0, fehler);

            KopieOhneErgebnisseMitEingaben(plan, stamm, variante, stammVorher, gesamtVorher,
                                           ausgenommen: SchemaKatalog.TAB_VARIANTE);
            Assert.Equal(stamm, Zahl("SELECT [ID_ProjektRef] FROM [Tab_Variante] WHERE [ID_Projekt] = ?", variante));
        }

        /// <summary>
        /// Die Kopie steht da wie ein nie gerechnetes Projekt: kein Lauf in
        /// <c>Tab_Ergebnis</c>, keine gespeicherte Wirtschaftlichkeit
        /// (<see cref="WirtschaftlichkeitCtrl.LadeErgebnisse"/> ist leer) — und keine Zeile
        /// der Datenbank zeigt auf den Lauf eines fremden Projekts.
        /// </summary>
        [Fact]
        public void Die_Kopie_steht_da_wie_ein_nie_gerechnetes_Projekt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Equal(3, Verweise(QUELLE).Count);
            int kopie = new ProjektDuplizierenCtrl().Duplizieren(QUELLNAME, "Verweisprobe Kopie");
            Assert.True(kopie > 0, "Duplizieren fehlgeschlagen.");

            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM [Tab_Ergebnis] WHERE [ID_Projekt] = ?", kopie));
            Assert.Empty(Verweise(kopie));
            Assert.Empty(new WirtschaftlichkeitCtrl().LadeErgebnisse(new List<int> { kopie }));

            // Die Quelle behält Lauf und Wirtschaftlichkeit samt Verweis.
            Assert.Equal(1, Zahl("SELECT COUNT(*) FROM [Tab_Ergebnis] WHERE [ID_Projekt] = ?", QUELLE));
            Assert.All(Verweise(QUELLE), v => Assert.Equal(LAUF_QUELLE, v));
            Assert.Equal(0, WirtschaftlichkeitFremdverweis.Offen());
        }

        /// <summary>
        /// ÜBERSICHT: Die frische Variante steht in der Datenlage der Vergleichsgruppe
        /// (<see cref="BerichtsDatenSammler.ErmittleStatus"/>, Quelle der Übersicht und des
        /// Berichtsdialogs) als „fehlt" — der Stamm mit seinem Lauf —, und der Lauf der
        /// Übersicht (<see cref="SimulationRunner.SimuliereUndSpeichere"/>) rechnet sie ohne
        /// Vorbedingung; danach trägt sie ihren eigenen Stand.
        /// </summary>
        [Fact]
        public void Eine_frische_Variante_steht_in_der_Uebersicht_als_fehlend_und_rechnet_mit()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            const string STAMM = "BHKW Test München";
            int stamm = new ProjektDuplizierenCtrl().GetProjektId(STAMM);
            int variante = new VariantenCtrl().AnlegenAusStamm(stamm, STAMM, "Frisch", out string fehler);
            Assert.True(variante > 0, fehler);

            var vorher = BerichtsDatenSammler.ErmittleStatus(stamm, STAMM).ToDictionary(s => s.IdProjekt);
            Assert.True(vorher[stamm].SimStand.HasValue, "Der Stamm hat seinen Lauf verloren.");
            Assert.False(vorher[variante].SimStand.HasValue, "Die Variante trägt den Lauf des Stamms.");
            Assert.Equal("— (fehlt) ⚠", vorher[variante].SimStandText);

            int lauf = new SimulationRunner().SimuliereUndSpeichere(variante, out string laufFehler);
            Assert.True(lauf > 0, "Lauf der Variante gescheitert: " + laufFehler);
            Assert.Equal(lauf, Zahl("SELECT [ID] FROM [Tab_Ergebnis] WHERE [ID_Projekt] = ?", variante));

            var nachher = BerichtsDatenSammler.ErmittleStatus(stamm, STAMM).ToDictionary(s => s.IdProjekt);
            Assert.True(nachher[variante].SimStand.HasValue);
            Assert.Equal(vorher[stamm].SimStand, nachher[stamm].SimStand);
        }

        /// <summary>
        /// DIE REGEL: Jede Tabelle, die ein Lauf oder die Wirtschaftlichkeitsrechnung
        /// schreibt, ist Ergebnis; keine Eingabetabelle ist es — auch nicht die Parameter der
        /// Wirtschaftlichkeit, die Tarife, die Kostenpositionen oder die Speicherauslegung.
        /// </summary>
        [Fact]
        public void Die_Regel_erkennt_jede_Ergebnistabelle_und_keine_Eingabe()
        {
            foreach (string t in ERGEBNISTABELLEN)
            {
                Assert.True(ProjektDuplizierenCtrl.IstErgebnisTabelle(t), t);
                Assert.True(ProjektDuplizierenCtrl.IstErgebnisTabelle(t.ToLowerInvariant()), t);
            }

            foreach (string t in new[] { "Tab_Projekt", "Tab_ProjektWerte", WirtschaftlichkeitCtrl.TAB_PARAMETER,
                                         WirtschaftlichkeitCtrl.TAB_TARIF, "Tab_Energieanlagen", "Z_AnlageSenke",
                                         "Tab_Einstellungen", "energy_project_settings", "energy_price",
                                         SchemaKatalog.TAB_VARIANTE, "Tab_SpeicherAuslegung", "Tab_Solar",
                                         "Tab_Klimadaten", "Tab_StromspeicherVariante", "Tab_Kostenprofil" })
                Assert.False(ProjektDuplizierenCtrl.IstErgebnisTabelle(t), t);
            Assert.False(ProjektDuplizierenCtrl.IstErgebnisTabelle(null));
        }

        /// <summary>
        /// DER PLAN kennzeichnet genau die Ergebnistabellen — und der Projekttransfer, der
        /// denselben Plan nimmt, führt sie weiter: Ein übertragenes Projekt behält seinen Lauf.
        /// </summary>
        [Fact]
        public void Der_Plan_kennzeichnet_genau_die_Ergebnistabellen_und_der_Transfer_behaelt_sie()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            List<ProjektDuplizierenCtrl.Spec> plan = new ProjektDuplizierenCtrl().ErmittlePlan();
            var erwartet = ERGEBNISTABELLEN.Where(TabelleVorhanden)
                                           .OrderBy(t => t, StringComparer.OrdinalIgnoreCase).ToList();
            var gekennzeichnet = plan.Where(s => s.Ergebnis).Select(s => s.Tabelle)
                                     .OrderBy(t => t, StringComparer.OrdinalIgnoreCase).ToList();
            Assert.Equal(erwartet, gekennzeichnet, StringComparer.OrdinalIgnoreCase);
            Assert.True(plan.Count(s => !s.Ergebnis) >= 40, "Zu wenige Eingabetabellen im Plan.");

            var transfer = new HashSet<string>(new ProjektExportImportCtrl().Transferplan().Select(s => s.Tabelle),
                                               StringComparer.OrdinalIgnoreCase);
            Assert.All(erwartet, t => Assert.Contains(t, transfer));
        }

        /// <summary>
        /// Eine DETAILTABELLE, die über ihren Fremdschlüssel an einem Lauf hängt, ist
        /// Ergebnis, auch ohne den Namensanfang — die Auto-Erkennung des Plans vererbt die
        /// Kennzeichnung, und die Kopie nimmt ihre Zeilen nicht mit.
        /// </summary>
        [Fact]
        public void Eine_Detailtabelle_eines_Laufs_ist_Ergebnis_auch_ohne_Namensanfang()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.True(DataRepository.ExecuteSQL(
                "CREATE TABLE [Tab_LaufdetailProbe] ([ID] INTEGER PRIMARY KEY, " +
                "[ID_Ergebnis] INTEGER REFERENCES [Tab_Ergebnis]([ID]) ON DELETE CASCADE, [Wert] REAL)"));
            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO [Tab_LaufdetailProbe] ([ID], [ID_Ergebnis], [Wert]) VALUES (1, ?, 1.5)",
                new DbParam("@lauf", LAUF_QUELLE)));

            ProjektDuplizierenCtrl.Spec spec = new ProjektDuplizierenCtrl().ErmittlePlan()
                .Single(s => string.Equals(s.Tabelle, "Tab_LaufdetailProbe", StringComparison.OrdinalIgnoreCase));
            Assert.True(spec.Ergebnis);

            int kopie = new ProjektDuplizierenCtrl().Duplizieren(QUELLNAME, "Verweisprobe Detail");
            Assert.True(kopie > 0, "Duplizieren fehlgeschlagen.");
            Assert.Equal(1, Zahl("SELECT COUNT(*) FROM [Tab_LaufdetailProbe]"));
            Assert.Equal(0, Zaehle(spec, kopie));
        }

        /// <summary>
        /// SICHERHEITSNETZ: Verweist eine EINGABEtabelle über einen deklarierten
        /// Fremdschlüssel auf eine Ergebnistabelle, wird der Verweis der Kopie leer — er
        /// zeigt weder auf den Lauf der Quelle (der nicht mitkommt) noch auf einen fremden.
        /// Die Testdatenbank kennt eine solche Spalte nicht; der Fall legt sie in der
        /// Arbeitskopie an.
        /// </summary>
        [Fact]
        public void Ein_Ergebnisverweis_einer_Eingabetabelle_wird_in_der_Kopie_leer()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.True(DataRepository.ExecuteSQL(
                "ALTER TABLE [Tab_Energieanlagen] ADD COLUMN [ID_ErgebnisProbe] INTEGER REFERENCES [Tab_Ergebnis]([ID])"));
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE [Tab_Energieanlagen] SET [ID_ErgebnisProbe] = ? WHERE [ID_Projekt] = ?",
                new DbParam("@lauf", LAUF_QUELLE), new DbParam("@p", QUELLE)));
            int anlagen = Zahl("SELECT COUNT(*) FROM [Tab_Energieanlagen] WHERE [ID_Projekt] = ?", QUELLE);
            Assert.True(anlagen > 0);

            int kopie = new ProjektDuplizierenCtrl().Duplizieren(QUELLNAME, "Verweisprobe Netz");
            Assert.True(kopie > 0, "Duplizieren fehlgeschlagen.");

            Assert.Equal(anlagen, Zahl("SELECT COUNT(*) FROM [Tab_Energieanlagen] WHERE [ID_Projekt] = ?", kopie));
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM [Tab_Energieanlagen] WHERE [ID_Projekt] = ? AND [ID_ErgebnisProbe] IS NOT NULL", kopie));
            Assert.Equal(anlagen, Zahl("SELECT COUNT(*) FROM [Tab_Energieanlagen] WHERE [ID_Projekt] = ? AND [ID_ErgebnisProbe] = ?",
                                       new DbParam("@p", QUELLE), new DbParam("@lauf", LAUF_QUELLE)));
        }

        // =================================================================================
        // 2 — Schemaschritt 106
        // =================================================================================

        /// <summary>
        /// Der Zielstand ist 106, und die Anweisung trifft genau einen gesetzten Verweis ohne
        /// Lauf desselben Projekts.
        /// </summary>
        [Fact]
        public void Der_Zielstand_ist_106_und_der_Schritt_trifft_nur_fremde_Verweise()
        {
            Assert.True(SchemaStand.Zielversion >= 106,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter 106.");
            Assert.Equal("Tab_ErgebnisWirtschaftlichkeit", WirtschaftlichkeitFremdverweis.TABELLE);
            Assert.Equal("ID_Ergebnis", WirtschaftlichkeitFremdverweis.SPALTE);
            Assert.StartsWith("UPDATE [Tab_ErgebnisWirtschaftlichkeit] SET [ID_Ergebnis] = NULL WHERE [ID_Ergebnis] > 0 AND NOT EXISTS",
                              WirtschaftlichkeitFremdverweis.SQL_SETZEN, StringComparison.Ordinal);
            Assert.Contains("e.[ID_Projekt] = [Tab_ErgebnisWirtschaftlichkeit].[ID_Projekt]",
                            WirtschaftlichkeitFremdverweis.SQL_SETZEN, StringComparison.Ordinal);
        }

        /// <summary>
        /// Die Arbeitskopie ist nachgezogen (<see cref="TestDatenbank"/>): Die 21 Zeilen
        /// stehen ohne Verweis da, die Zeilen mit eigenem Lauf behalten ihn — 1026 auf 167,
        /// 1042 auf 206, 1030 auf 212 hat keine, 1027 auf 168.
        /// </summary>
        [Fact]
        public void Die_21_Zeilen_der_Testdatenbank_stehen_ohne_fremden_Verweis()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Equal(0, WirtschaftlichkeitFremdverweis.Offen());
            Assert.Empty(WirtschaftlichkeitFremdverweis.Betroffene());

            foreach (int id in FREMD)
                Assert.True(Verweis(id) == null, "Zeile " + id + ": Verweis ist nicht NULL.");

            Assert.All(Verweise(QUELLE), v => Assert.Equal(LAUF_QUELLE, v));
            Assert.All(Verweise(1042), v => Assert.Equal(206, v));
            Assert.All(Verweise(1027), v => Assert.Equal(168, v));
        }

        /// <summary>
        /// Die Anweisung an drei Zeilen: ein Verweis auf den Lauf eines fremden Projekts und
        /// einer auf einen Lauf, den es nicht gibt, werden NULL; der eigene bleibt. Der zweite
        /// Lauf findet nichts mehr.
        /// </summary>
        [Fact]
        public void Der_Schritt_leert_fremde_Verweise_und_ist_wiederholbar()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            const int FREMD_ZEILE = 16, VERWAIST_ZEILE = 18, EIGEN_ZEILE = 213;   // 1028, 1028, 1043
            Setze(FREMD_ZEILE, LAUF_QUELLE);   // Projekt 1028 -> Lauf von 1026
            Setze(VERWAIST_ZEILE, 999999);     // Projekt 1028 -> kein Lauf
            Setze(EIGEN_ZEILE, 209);           // Projekt 1043 -> eigener Lauf 209

            Assert.Equal(2, WirtschaftlichkeitFremdverweis.Offen());
            List<string> betroffene = WirtschaftlichkeitFremdverweis.Betroffene();
            Assert.Equal(new[] { "Id 16, Projekt 1028: Lauf 167 (Erwartet)",
                                 "Id 18, Projekt 1028: Lauf 999999 (Best)" }, betroffene);
            Assert.Single(WirtschaftlichkeitFremdverweis.Anweisungen);

            Assert.Equal(2, WirtschaftlichkeitFremdverweis.Ausfuehren());

            Assert.Null(Verweis(FREMD_ZEILE));
            Assert.Null(Verweis(VERWAIST_ZEILE));
            Assert.Equal(209, Verweis(EIGEN_ZEILE));

            // WIEDERHOLBAR: nichts mehr offen, keine Anweisung, nichts gesetzt.
            Assert.Equal(0, WirtschaftlichkeitFremdverweis.Offen());
            Assert.Empty(WirtschaftlichkeitFremdverweis.Anweisungen);
            Assert.Equal(0, WirtschaftlichkeitFremdverweis.Ausfuehren());
            Assert.Equal(209, Verweis(EIGEN_ZEILE));
        }

        /// <summary>Ohne die (lazy angelegte) Tabelle tut der Schritt nichts.</summary>
        [Fact]
        public void Ohne_Tabelle_tut_der_Schritt_nichts()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.True(DataRepository.ExecuteSQL("DROP TABLE [Tab_ErgebnisWirtschaftlichkeit]"));

            Assert.False(WirtschaftlichkeitFremdverweis.Vorhanden());
            Assert.Equal(0, WirtschaftlichkeitFremdverweis.Offen());
            Assert.Empty(WirtschaftlichkeitFremdverweis.Betroffene());
            Assert.Empty(WirtschaftlichkeitFremdverweis.Anweisungen);
            Assert.Equal(0, WirtschaftlichkeitFremdverweis.Ausfuehren());
        }

        // =================================================================================
        // Hilfen
        // =================================================================================

        /// <summary>
        /// Die Ergebnistabellen, die ein Simulationslauf (<see cref="ErgebnisCtrl"/>) und die
        /// Wirtschaftlichkeitsrechnung (<see cref="WirtschaftlichkeitCtrl"/>) schreiben — als
        /// eigenes Orakel des Nachweises ausgeschrieben, nicht aus der Regel abgeleitet.
        /// </summary>
        internal static readonly string[] ERGEBNISTABELLEN =
        {
            ErgebnisCtrl.TAB_KOPF, ErgebnisCtrl.TAB_ENERGIE,
            ErgebnisCtrl.TAB_WP, ErgebnisCtrl.TAB_WP_MODUL,
            ErgebnisCtrl.TAB_BHKW, ErgebnisCtrl.TAB_BHKW_MODUL,
            ErgebnisCtrl.TAB_KESSEL, ErgebnisCtrl.TAB_KESSEL_MODUL,
            ErgebnisCtrl.TAB_SOLAR, ErgebnisCtrl.TAB_SOLAR_MODUL,
            ErgebnisCtrl.TAB_PV, ErgebnisCtrl.TAB_PV_MODUL,
            ErgebnisCtrl.TAB_PUFFER, ErgebnisCtrl.TAB_SP, ErgebnisCtrl.TAB_GEB,
            WirtschaftlichkeitCtrl.TAB_ERGEBNIS, WirtschaftlichkeitCtrl.TAB_SENS, WirtschaftlichkeitCtrl.TAB_MATRIX,
            // Gebaeudesimulation G6b (Schritt S-G, A6): das Ergebnis je Zone am Gebaeudeergebnis.
            SchemaKatalog.TAB_ERGEBNISZONE
        };

        private static bool Ergebnistabelle(string tabelle)
            => ERGEBNISTABELLEN.Contains(tabelle, StringComparer.OrdinalIgnoreCase);

        private static Dictionary<string, int> ZaehleJeTabelle(List<ProjektDuplizierenCtrl.Spec> plan, int projekt)
        {
            var z = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in plan) z[s.Tabelle] = Zaehle(s, projekt);
            return z;
        }

        /// <summary>Zeilen je Ergebnistabelle über ALLE Projekte — eine Kopie darf keine hinzufügen.</summary>
        private static Dictionary<string, int> GesamtJeErgebnistabelle(List<ProjektDuplizierenCtrl.Spec> plan)
        {
            var z = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in plan)
                if (Ergebnistabelle(s.Tabelle)) z[s.Tabelle] = Zahl("SELECT COUNT(*) FROM [" + s.Tabelle + "]");
            return z;
        }

        /// <summary>
        /// Die Ausgangslage — ohne sie belegt der Fall nichts: Die Quelle hat einen Lauf mit
        /// Detailzeilen, eine gespeicherte Wirtschaftlichkeit, Anlagen, Senken,
        /// Kostenpositionen und Zuordnungen; der Plan kennt jede Ergebnistabelle.
        /// </summary>
        private static void QuelleTraegtLaufUndEingaben(Dictionary<string, int> quelle)
        {
            foreach (string t in new[] { ErgebnisCtrl.TAB_KOPF, ErgebnisCtrl.TAB_ENERGIE, ErgebnisCtrl.TAB_KESSEL,
                                         ErgebnisCtrl.TAB_KESSEL_MODUL, WirtschaftlichkeitCtrl.TAB_ERGEBNIS,
                                         "Tab_Energieanlagen", "Z_AnlageSenke", "Tab_ProjektWerte",
                                         "Z_ProjektGebaeude", "Tab_Einstellungen", "Tab_Pufferspeicher" })
                Assert.True(quelle.TryGetValue(t, out int n) && n > 0, t + ": die Quelle führt keine Zeile.");

            foreach (string t in ERGEBNISTABELLEN)
                if (TabelleVorhanden(t))
                    Assert.True(quelle.ContainsKey(t), t + " fehlt im Kopierplan — der Nachweis sähe sie nicht.");
        }

        /// <summary>
        /// Der Kern des Nachweises: Ergebnistabellen der Kopie leer, Eingabetabellen der Kopie
        /// gleich der Quelle (außer <paramref name="ausgenommen"/>), die Quelle in JEDER Tabelle
        /// unverändert, und keine Ergebnistabelle ist insgesamt gewachsen.
        /// </summary>
        private static void KopieOhneErgebnisseMitEingaben(List<ProjektDuplizierenCtrl.Spec> plan,
            int quelle, int kopie, Dictionary<string, int> quelleVorher,
            Dictionary<string, int> gesamtVorher, string ausgenommen)
        {
            int eingaben = 0;
            foreach (var s in plan)
            {
                int n = Zaehle(s, kopie);
                if (Ergebnistabelle(s.Tabelle))
                    Assert.True(n == 0, s.Tabelle + ": die Kopie führt " + n + " Ergebniszeilen der Quelle.");
                else if (!string.Equals(s.Tabelle, ausgenommen, StringComparison.OrdinalIgnoreCase))
                {
                    Assert.True(n == quelleVorher[s.Tabelle],
                                s.Tabelle + ": Quelle " + quelleVorher[s.Tabelle] + ", Kopie " + n + ".");
                    if (n > 0) eingaben++;
                }

                Assert.True(Zaehle(s, quelle) == quelleVorher[s.Tabelle],
                            s.Tabelle + ": das Kopieren hat die QUELLE verändert.");
            }
            Assert.True(eingaben >= 10, "Nur " + eingaben + " Eingabetabellen mit Zeilen kopiert.");

            foreach (KeyValuePair<string, int> kv in gesamtVorher)
                Assert.True(Zahl("SELECT COUNT(*) FROM [" + kv.Key + "]") == kv.Value,
                            kv.Key + ": das Kopieren hat Ergebniszeilen hinzugefügt.");
        }

        private static bool TabelleVorhanden(string tabelle)
            => Zahl("SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = ?",
                    new DbParam("@t", tabelle)) > 0;

        private static int Zaehle(ProjektDuplizierenCtrl.Spec spec, int projekt)
            => Zahl("SELECT COUNT(*) FROM [" + spec.Tabelle + "] WHERE " + string.Format(spec.Filter, projekt));

        private static int Zahl(string sql, int projekt) => Zahl(sql, new DbParam("@p", projekt));

        private static int Zahl(string sql, params DbParam[] p)
        {
            object o = DataRepository.ExecuteScalar(sql, p);
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o, CultureInfo.InvariantCulture);
        }

        private static List<int?> Verweise(int projekt)
        {
            DataTable t = DataRepository.GetDataTable(
                "SELECT [ID_Ergebnis] FROM [Tab_ErgebnisWirtschaftlichkeit] WHERE [ID_Projekt] = ? ORDER BY [ID]",
                new DbParam("@p", projekt));
            var liste = new List<int?>();
            if (t == null) return liste;
            foreach (DataRow r in t.Rows)
                liste.Add(r[0] == DBNull.Value ? (int?)null : Convert.ToInt32(r[0], CultureInfo.InvariantCulture));
            return liste;
        }

        private static int? Verweis(int zeile)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT [ID_Ergebnis] FROM [Tab_ErgebnisWirtschaftlichkeit] WHERE [ID] = ?",
                new DbParam("@id", zeile));
            return o == null || o == DBNull.Value ? (int?)null : Convert.ToInt32(o, CultureInfo.InvariantCulture);
        }

        private static void Setze(int zeile, int lauf)
        {
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE [Tab_ErgebnisWirtschaftlichkeit] SET [ID_Ergebnis] = ? WHERE [ID] = ?",
                new DbParam("@lauf", lauf), new DbParam("@id", zeile)));
        }
    }
}
