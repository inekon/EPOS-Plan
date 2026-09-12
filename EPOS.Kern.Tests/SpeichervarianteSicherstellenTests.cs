using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ANWENDERBEFUND 10.09.2026 (W11b‑B‑27): „Simulation → Detaillierte Simulation →
    /// Parameter → Stromspeicher: Nach Auslegung optimieren: Keine aktive Speichervariante –
    /// die Eingaben sind gesperrt."
    ///
    /// <para><b>Der Befund.</b> Projekt 1050 führte EINE Speicheranlage
    /// (<c>ID_Type</c> = <c>SP_TYP</c>), aber KEINE Zeile in
    /// <c>Tab_StromspeicherVariante</c>. Ohne sie sperrt der Reiter „Parameter" seine
    /// Eingaben (<c>SpAktiv =&gt; !Gesperrt &amp;&amp; Sp.VarianteVorhanden</c>), und der
    /// Leistungspreis aus dem Optimierungsdialog wird still nicht gespeichert.</para>
    ///
    /// <para><b>Zwei Gründe.</b> (1) Der Del+Add-Speicherweg rettet nur VORHANDENE
    /// Variantenzeilen — beim ERSTEN Speicher eines Projekts gibt es nichts zu sichern,
    /// und <c>WizardCtrl.SpVariantenWiederherstellen</c> stieg dann sofort aus. (2) Die
    /// Selbstheilung des WinForms-Altzweigs
    /// (<c>StromspeicherKontextMenuCtrl.VarianteSicherstellen</c> /
    /// <c>AktiveVarianteSicherstellen</c>) fiel mit Commit <c>55a3f0ec</c>; der Razor-Weg
    /// hatte keinen Ersatz.</para>
    ///
    /// <para><b>Was hier geprüft wird.</b> Die neue Nachführung
    /// <c>StromspeicherVarianteCtrl.AktiveVarianteSicherstellen</c> (Abschnitt 1) und der
    /// Speicherweg des Wizards selbst (Abschnitt 2) — samt der Zusage, dass die Rettung
    /// vorhandener Betriebsparameter dabei nicht verloren geht.</para>
    ///
    /// <para><b>Warum ein SYNTHETISCHES Projekt.</b> Wie in
    /// <see cref="BetriebskostenBaugroesseTests"/>: Die Testdatenbank führt kein Projekt
    /// mit einer Speicheranlage OHNE Variantenzeile — Migrationsschritt 11d hat jede
    /// Bestandsanlage versorgt. Die Fälle legen den Zustand des Befundes deshalb in der
    /// ARBEITSKOPIE selbst an. Die Schlüssel liegen weit oberhalb des Bestands
    /// (Höchststände am 10.09.2026: Projekt 1045, Anlage 14931, Gerät 1017061,
    /// Variante 13).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class SpeichervarianteSicherstellenTests
    {
        private const int PROJEKT = 191001;

        private const int A_SP1 = 191101;
        private const int A_SP2 = 191102;
        private const int A_REF = 191103;
        private const int A_WP = 191104;

        private const int G_SP1 = 191201;
        private const int G_SP2 = 191202;
        private const int G_WP = 191203;

        /// <summary>
        /// Zwei Bezeichner, die im KATALOG <c>Tab_Stromspeicher_STAMM</c> der
        /// Testdatenbank stehen. Der Wizard-Schreibweg löst die Gerätekopie über
        /// <c>StromspeicherCtrl.CopyFromStamm(Bezeichner, Projekt)</c> auf; ein
        /// Phantasiename ließe <c>ID_SP</c> auf 0 stehen, und der Fremdschlüssel der
        /// Anlagenzeile liest die 0 als Verweis auf ein Gerät, das es nicht gibt.
        /// </summary>
        private const string KATALOG_A = "BYD B-Box HVM 11.0";
        private const string KATALOG_B = "VARTA pulse neo";

        // =====================================================================
        // Aufbau
        // =====================================================================

        private static void ProjektAnlegen()
        {
            Sql("INSERT INTO Tab_Projekt (ID, Projektname) VALUES (" +
                PROJEKT + ", 'W11b-B-27 Speichervariante')");
        }

        private static void SpeicherGeraet(int id, string bezeichner)
        {
            Sql("INSERT INTO Tab_Stromspeicher (ID, ID_Projekt, Bezeichner, Leistung, Energie) VALUES (" +
                id + ", " + PROJEKT + ", '" + bezeichner + "', " + Z(100.0) + ", " + Z(129.0) + ")");
        }

        /// <summary>Die sieben Geräte-Verweisspalten der Anlagenzeile. Sie müssen ALLE
        /// gesetzt werden — ihr Spaltenvorgabewert ist 0, und die Fremdschlüssel der
        /// SQLite-Fassung lesen die 0 als echten Verweis auf ein Gerät, das es nicht gibt
        /// (Begründung wortgleich in <see cref="BetriebskostenBaugroesseTests"/>).</summary>
        private static readonly string[] GERAETEVERWEISE =
        { "ID_WP", "ID_Kessel", "ID_BHKW", "ID_PV", "ID_Solar", "ID_SP", "ID_PUFFER" };

        private static void Anlage(int id, string bezeichner, int typ, string verweisSpalte, int geraet)
        {
            string spalten = "ID, ID_Projekt, Bezeichner, ID_Type";
            string werte = id + ", " + PROJEKT + ", '" + bezeichner + "', " + typ;

            foreach (string s in GERAETEVERWEISE)
            {
                spalten += ", [" + s + "]";
                werte += ", " + (string.Equals(s, verweisSpalte, StringComparison.Ordinal)
                                 ? geraet.ToString(CultureInfo.InvariantCulture) : "NULL");
            }

            Sql("INSERT INTO Tab_Energieanlagen (" + spalten + ") VALUES (" + werte + ")");
        }

        private static void Speicheranlage(int id, string bezeichner, int geraet)
        {
            SpeicherGeraet(geraet, bezeichner);
            Anlage(id, bezeichner, WizardItemClass.SP_TYP, "ID_SP", geraet);
        }

        /// <summary>Ein Dialogeintrag für den Wizard-Schreibweg — ohne Anlagen-Id und ohne
        /// Geräte-Id: beide entstehen erst dort.</summary>
        private static WErzeugerModel SpEintrag(string bezeichner)
        {
            return new WErzeugerModel
            {
                ID_Projekt = PROJEKT,
                Bezeichner = bezeichner,
                ID_Type = WizardItemClass.SP_TYP
            };
        }

        // =====================================================================
        // 1 — AktiveVarianteSicherstellen
        // =====================================================================

        /// <summary>
        /// <b>Der Befund als Kernprobe.</b> Eine Speicheranlage ohne Variantenzeile
        /// bekommt eine — mit der Vorbelegung des Modells, und sie ist aktiv.
        /// </summary>
        [Fact]
        public void Eine_Speicheranlage_ohne_Zeile_bekommt_eine_aktive_Variante()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ProjektAnlegen();
            Speicheranlage(A_SP1, "Speicher A", G_SP1);

            // Der Zustand des Befundes: Anlage ja, Variante nein.
            Assert.Null(new StromspeicherVarianteCtrl().ReadAktiveVariante(PROJEKT));

            StromspeicherVarianteModel v = new StromspeicherVarianteCtrl().AktiveVarianteSicherstellen(PROJEKT);

            Assert.NotNull(v);
            Assert.Equal(A_SP1, v.ID_Energieanlage);
            Assert.True(v.Aktiv);

            // Die Vorbelegung des Modells — dieselben Werte wie aus Migrationsschritt 11d.
            Assert.Equal(StromspeicherVarianteModel.SOC_MIN_VORGABE, v.SoC_Min_Prozent, 6);
            Assert.Equal(StromspeicherVarianteModel.SOC_MAX_VORGABE, v.SoC_Max_Prozent, 6);
            Assert.Equal(StromspeicherVarianteModel.KAPITALZINS_VORGABE, v.Kapitalzins, 6);
            Assert.Equal(DbWerte.SP_BETRIEBSART_GRUENSTROM, v.Betriebsart);

            Assert.Single(new StromspeicherVarianteCtrl().ReadAllByProjekt(PROJEKT));
        }

        /// <summary>
        /// Zwei Anlagen ohne Zeile bekommen ZWEI Zeilen — aktiv wird die der ERSTEN in
        /// Anlagenreihenfolge (<c>ORDER BY ID</c>): dieselbe Wahl wie Migrationsschritt 11d
        /// und wie der frühere <c>StromspeicherKontextMenuCtrl</c>.
        /// </summary>
        [Fact]
        public void Zwei_Anlagen_ohne_Zeile_bekommen_zwei_Zeilen_und_die_erste_wird_aktiv()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ProjektAnlegen();
            Speicheranlage(A_SP1, "Speicher A", G_SP1);
            Speicheranlage(A_SP2, "Speicher B", G_SP2);

            StromspeicherVarianteModel v = new StromspeicherVarianteCtrl().AktiveVarianteSicherstellen(PROJEKT);

            Assert.NotNull(v);
            Assert.Equal(A_SP1, v.ID_Energieanlage);

            List<StromspeicherVarianteModel> alle = new StromspeicherVarianteCtrl().ReadAllByProjekt(PROJEKT);
            Assert.Equal(2, alle.Count);
            Assert.Single(alle.FindAll(x => x.Aktiv));      // genau eine aktive
        }

        /// <summary>
        /// Zeilen vorhanden, aber keine aktiv (der zweite Fall des alten
        /// <c>AktiveVarianteSicherstellen</c>): Die erste wird aktiviert, und es entsteht
        /// KEINE neue Zeile.
        /// </summary>
        [Fact]
        public void Vorhandene_Zeilen_ohne_aktive_bekommen_keine_neue_Zeile()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ProjektAnlegen();
            Speicheranlage(A_SP1, "Speicher A", G_SP1);
            Speicheranlage(A_SP2, "Speicher B", G_SP2);

            var ctrl = new StromspeicherVarianteCtrl();
            Assert.True(ctrl.Insert(new StromspeicherVarianteModel { ID_Energieanlage = A_SP1 }) > 0);
            Assert.True(ctrl.Insert(new StromspeicherVarianteModel { ID_Energieanlage = A_SP2 }) > 0);

            int vorher = Zeilen();
            Assert.Null(new StromspeicherVarianteCtrl().ReadAktiveVariante(PROJEKT));

            StromspeicherVarianteModel v = new StromspeicherVarianteCtrl().AktiveVarianteSicherstellen(PROJEKT);

            Assert.NotNull(v);
            Assert.Equal(A_SP1, v.ID_Energieanlage);
            Assert.Equal(vorher, Zeilen());
        }

        /// <summary>
        /// Ohne Speicheranlage gibt es nichts nachzuziehen — <c>null</c>, und die Tabelle
        /// bleibt unberührt. Eine Variante ohne Anlage wäre eine Waise.
        /// </summary>
        [Fact]
        public void Ohne_Speicheranlage_bleibt_die_Variantentabelle_unberuehrt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ProjektAnlegen();
            Sql("INSERT INTO Tab_WP (ID, ID_Projekt, Bezeichner, Nennleistung) VALUES (" +
                G_WP + ", " + PROJEKT + ", 'WP 12', " + Z(12.0) + ")");
            Anlage(A_WP, "Wärmepumpe", WizardItemClass.WP_TYP, "ID_WP", G_WP);

            int vorher = Zeilen();

            Assert.Null(new StromspeicherVarianteCtrl().AktiveVarianteSicherstellen(PROJEKT));
            Assert.Equal(vorher, Zeilen());
        }

        /// <summary>
        /// <b>Die Referenzliste bleibt draußen.</b> Eine <c>REF_SP_TYP</c>-Anlage führt den
        /// VERGLEICHSFALL des Projekts, keine Planvariante — dieselbe Grenze, die auch die
        /// Ersatzwahl in <c>WizardCtrl.SpVariantenWiederherstellen</c> zieht.
        /// </summary>
        [Fact]
        public void Eine_Referenzanlage_allein_bekommt_keine_Variante()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ProjektAnlegen();
            SpeicherGeraet(G_SP1, "Vergleichsspeicher");
            Anlage(A_REF, "Vergleichsspeicher", WizardItemClass.REF_SP_TYP, "ID_SP", G_SP1);

            int vorher = Zeilen();

            Assert.Null(new StromspeicherVarianteCtrl().AktiveVarianteSicherstellen(PROJEKT));
            Assert.Equal(vorher, Zeilen());
        }

        /// <summary>
        /// <b>Idempotent.</b> Der zweite Aufruf findet die aktive Variante vor und schreibt
        /// nichts — geprüft über ein Abbild ALLER Spalten ALLER Zeilen.
        /// </summary>
        [Fact]
        public void Der_zweite_Aufruf_laesst_die_Zeilen_bitgleich()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ProjektAnlegen();
            Speicheranlage(A_SP1, "Speicher A", G_SP1);
            Speicheranlage(A_SP2, "Speicher B", G_SP2);

            Assert.NotNull(new StromspeicherVarianteCtrl().AktiveVarianteSicherstellen(PROJEKT));
            string nachDemErsten = Abbild();

            StromspeicherVarianteModel zweiter = new StromspeicherVarianteCtrl().AktiveVarianteSicherstellen(PROJEKT);

            Assert.NotNull(zweiter);
            Assert.Equal(nachDemErsten, Abbild());
        }

        /// <summary>Ohne Projekt-Id passiert nichts — kein Schreibversuch, keine Ausnahme.</summary>
        [Fact]
        public void Ohne_Projekt_wird_nichts_geschrieben()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ProjektAnlegen();
            Speicheranlage(A_SP1, "Speicher A", G_SP1);

            int vorher = Zeilen();

            Assert.Null(new StromspeicherVarianteCtrl().AktiveVarianteSicherstellen(0));
            Assert.Null(new StromspeicherVarianteCtrl().AktiveVarianteSicherstellen(-3));
            Assert.Equal(vorher, Zeilen());
        }

        // =====================================================================
        // 2 — Der Speicherweg des Wizards (Del + Add)
        // =====================================================================

        /// <summary>
        /// <b>DER BEFUND SELBST.</b> Ein frisches Projekt, der Speicherweg mit EINEM
        /// Speicher-Eintrag — danach führt die neue Anlagenzeile genau eine Variante, und
        /// sie ist aktiv. VOR W11b‑B‑27 blieb sie aus: Es gab nichts zu sichern, und
        /// <c>SpVariantenWiederherstellen</c> stieg vor der Schleife aus.
        /// </summary>
        [Fact]
        public void Der_erste_Speicher_eines_Projekts_bekommt_seine_Variante()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ProjektAnlegen();

            // Der Speicherweg, Zeichen für Zeichen: erst der typgefilterte Löschbefehl
            // (er trifft nichts — das Projekt ist neu), dann die Dialogliste.
            var wizard = new WizardCtrl();
            Assert.True(wizard.Del_Projekt_Waermeerzeuger(PROJEKT, WizardItemClass.SP_TYP));
            Assert.True(wizard.Add_WP_Waermeerzeuger(PROJEKT,
                new List<WErzeugerModel> { SpEintrag(KATALOG_A) }));

            int idAnlage = AnlagenId(KATALOG_A);
            Assert.True(idAnlage > 0);

            List<StromspeicherVarianteModel> alle = new StromspeicherVarianteCtrl().ReadAllByProjekt(PROJEKT);
            Assert.Single(alle);
            Assert.Equal(idAnlage, alle[0].ID_Energieanlage);

            StromspeicherVarianteModel aktiv = new StromspeicherVarianteCtrl().ReadAktiveVariante(PROJEKT);
            Assert.NotNull(aktiv);
            Assert.Equal(idAnlage, aktiv.ID_Energieanlage);
        }

        /// <summary>
        /// <b>Regressionsschutz für die Rettung (AP9b).</b> Ein Projekt mit gepflegter
        /// aktiver Variante, derselbe Speicherweg noch einmal: Die Betriebsparameter
        /// stehen danach an der NEUEN Anlagenzeile, und sie ist wieder aktiv.
        /// </summary>
        [Fact]
        public void Der_Speicherweg_traegt_Betriebsparameter_und_Aktivmarkierung_weiter()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ProjektAnlegen();

            var wizard = new WizardCtrl();
            var liste = new List<WErzeugerModel> { SpEintrag(KATALOG_A) };
            Assert.True(wizard.Add_WP_Waermeerzeuger(PROJEKT, liste));

            var ctrl = new StromspeicherVarianteCtrl();
            StromspeicherVarianteModel gepflegt = ctrl.ReadAktiveVariante(PROJEKT);
            Assert.NotNull(gepflegt);
            gepflegt.SoC_Min_Prozent = 42.0;
            gepflegt.Nutzungsdauer = 17.0;
            Assert.True(ctrl.Update(gepflegt));

            Assert.True(wizard.Del_Projekt_Waermeerzeuger(PROJEKT, WizardItemClass.SP_TYP));
            Assert.True(wizard.Add_WP_Waermeerzeuger(PROJEKT, liste));

            StromspeicherVarianteModel danach = new StromspeicherVarianteCtrl().ReadAktiveVariante(PROJEKT);

            Assert.NotNull(danach);
            Assert.Equal(AnlagenId(KATALOG_A), danach.ID_Energieanlage);
            Assert.Equal(42.0, danach.SoC_Min_Prozent, 6);
            Assert.Equal(17.0, danach.Nutzungsdauer, 6);
            Assert.Single(new StromspeicherVarianteCtrl().ReadAllByProjekt(PROJEKT));
        }

        /// <summary>
        /// <b>Ein ZWEITER Speicher nimmt der aktiven Variante die Markierung nicht.</b> Der
        /// neue Eintrag steht in der Dialogliste VORNE und bekäme damit die kleinere
        /// Anlagen-Id — die Ersatzwahl „erste Anlage" zeigte auf ihn. Maßgeblich bleibt die
        /// gerettete Aktivmarkierung, zugeordnet über den Bezeichner.
        /// </summary>
        [Fact]
        public void Ein_zweiter_Speicher_nimmt_der_aktiven_Variante_die_Markierung_nicht()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ProjektAnlegen();

            var wizard = new WizardCtrl();
            WErzeugerModel ersteWahl = SpEintrag(KATALOG_A);
            Assert.True(wizard.Add_WP_Waermeerzeuger(PROJEKT, new List<WErzeugerModel> { ersteWahl }));
            Assert.NotNull(new StromspeicherVarianteCtrl().ReadAktiveVariante(PROJEKT));

            WErzeugerModel dazu = SpEintrag(KATALOG_B);
            Assert.True(wizard.Del_Projekt_Waermeerzeuger(PROJEKT, WizardItemClass.SP_TYP));
            Assert.True(wizard.Add_WP_Waermeerzeuger(PROJEKT,
                new List<WErzeugerModel> { dazu, ersteWahl }));

            List<StromspeicherVarianteModel> alle = new StromspeicherVarianteCtrl().ReadAllByProjekt(PROJEKT);
            Assert.Equal(2, alle.Count);
            Assert.Single(alle.FindAll(x => x.Aktiv));

            StromspeicherVarianteModel aktiv = new StromspeicherVarianteCtrl().ReadAktiveVariante(PROJEKT);
            Assert.NotNull(aktiv);
            Assert.Equal(KATALOG_A, AnlagenBezeichner(aktiv.ID_Energieanlage));
        }

        // =====================================================================
        // Werkzeug
        // =====================================================================

        /// <summary>Alle Spalten aller Variantenzeilen als Text — die Wache unter jedem
        /// „es hat sich nichts geändert" dieser Datei.</summary>
        private static string Abbild()
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM [" + StromspeicherVarianteCtrl.TABLE + "] ORDER BY ID");

            var sb = new StringBuilder();
            if (dt == null) return sb.ToString();

            foreach (DataRow r in dt.Rows)
            {
                foreach (DataColumn c in dt.Columns)
                    sb.Append(c.ColumnName).Append('=')
                      .Append(Convert.ToString(r[c], CultureInfo.InvariantCulture)).Append(';');
                sb.Append('\n');
            }

            return sb.ToString();
        }

        private static int Zeilen()
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM [" + StromspeicherVarianteCtrl.TABLE + "]");
            return (v == null || v == DBNull.Value) ? 0 : Convert.ToInt32(v);
        }

        private static int AnlagenId(string bezeichner)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND Bezeichner = ?",
                new DbParam("@p", PROJEKT), new DbParam("@b", bezeichner));
            return (v == null || v == DBNull.Value) ? 0 : Convert.ToInt32(v);
        }

        private static string AnlagenBezeichner(int idAnlage)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT Bezeichner FROM Tab_Energieanlagen WHERE ID = ?",
                new DbParam("@id", idAnlage));
            return (v == null || v == DBNull.Value) ? "" : Convert.ToString(v);
        }

        /// <summary>Eine Zahl als SQL-LITERAL, zwingend invariant — unter de-DE machte
        /// <c>ToString()</c> aus 100,0 ein „100,0", und das wäre im INSERT eine Spalte zu
        /// viel (Begründung wortgleich in <see cref="BetriebskostenBaugroesseTests"/>).</summary>
        private static string Z(double wert)
        {
            return wert.ToString(CultureInfo.InvariantCulture);
        }

        private static void Sql(string sql) { DataRepository.ExecuteSQL(sql); }
    }
}
