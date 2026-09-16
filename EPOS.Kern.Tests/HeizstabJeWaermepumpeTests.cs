using System;
using System.Data;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Heizstab gehoert der Waermepumpe, nicht dem Projekt</b>
    /// (Anwenderentscheid 16.09.2026, Auftrag #299) — Schemaschritt 79 und der
    /// Rechenweg, der daraus folgt.
    ///
    /// <para><b>Warum es diese Klasse gibt.</b> Bis zum Entscheid las der Lauf
    /// ausschliesslich die PROJEKTeinstellung <c>Tab_Einstellungen.WP_Heizstab</c>; der
    /// gleichnamige Schalter an der Anlagenzeile entschied ueber gar nichts, was
    /// gerechnet wird. Ein Projekt mit zwei Waermepumpen konnte den Heizstab deshalb nur
    /// gemeinsam ein- oder ausschalten. Dass jetzt JEDE Waermepumpe ihren eigenen
    /// Schalter hat, ist am Referenzlauf NICHT abzulesen: Er rechnet einen bestehenden
    /// Stand nach, und die Migration hat genau dafuer gesorgt, dass sich dabei nichts
    /// aendert. Geprueft wird deshalb hier.</para>
    ///
    /// <para><b>Diese Klasse SCHREIBT</b> und braucht ihre eigene Arbeitskopie;
    /// <see cref="TestDatenbank"/> als <c>IClassFixture</c> legt je Testklasse eine an.
    /// <c>Referenzlaeufe/Kenndaten_Test.sqlite</c> bleibt unberuehrt. Fehlt die Datei,
    /// schweigen die Faelle.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class HeizstabJeWaermepumpeTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;

        public HeizstabJeWaermepumpeTests(TestDatenbank db) { _db = db; }

        // --- Projekt 1019 fuehrt ZWEI Waermepumpen-Anlagen, beide mit Kennlinien und
        //     einer Heizstableistung (Tab_WP.Heizung 6 bzw. 9 kW). Genau das braucht der
        //     Rechenwegfall: zwei Module im selben Lauf, einer mit, einer ohne Heizstab.
        private const int PROJEKT_ZWEI_WP = 1019;
        private const int WP_A = 14922;   // CS6800iAW MB + AW 10 OR-T, Heizung 6 kW
        private const int WP_B = 14923;   // CS7800iLW 12,               Heizung 9 kW

        // --- Projekt 1017 fuehrt EINE Waermepumpe und rechnete OHNE Heizstab.
        private const int PROJEKT_OHNE = 1017;
        private const int WP_OHNE = 10211;

        // =================================================================================
        // 1 - Der Migrationsschritt
        // =================================================================================

        /// <summary>
        /// Die Uebernahme gibt JEDER Waermepumpen-Anlage den Wert des Projektschalters
        /// ihres Projekts, laesst andere Anlagenarten in Ruhe — und danach ist die
        /// Projektspalte weg.
        /// </summary>
        /// <remarks>
        /// <para><b>EIN Fall fuer den ganzen Schritt, mit Absicht.</b> Die Arbeitskopie
        /// steht bereits auf dem Zielstand: Die Spalte ist entfernt und die Uebernahme
        /// gelaufen. Der Fall stellt den Ausgangszustand deshalb selbst her (Spalte
        /// zurueck, Werte gesaet, Anlagenschalter auf 0) und faehrt dann DIESELBEN
        /// Anweisungen, die Migration und Werkzeug fahren. Zwei getrennte Faelle — einer
        /// fuer die Uebernahme, einer fuer das Entfernen — teilten sich dieselbe Kopie
        /// und liefen in keiner zugesicherten Reihenfolge.</para>
        /// </remarks>
        [Fact]
        public void Der_Schritt_uebergibt_den_Projektschalter_und_entfernt_ihn_danach()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            // --- Ausgangszustand: der Projektschalter steht wieder, die Anlagen auf 0 ---
            ProjektschalterHerstellen();

            Sql("UPDATE Tab_Energieanlagen SET Heizstab = 0");
            Sql("UPDATE Tab_Einstellungen SET WP_Heizstab = 0");
            Sql("UPDATE Tab_Einstellungen SET WP_Heizstab = 1 WHERE ID_Projekt = ?",
                new DbParam("@p", PROJEKT_ZWEI_WP));

            // Eine NICHT-Waermepumpe im selben Projekt - sie darf der Schritt nicht
            // anfassen. Projekt 1019 fuehrt zwei Pufferspeicher-Anlagen; die erste ist
            // der Zeuge. Zur Sicherheit steht ihr Schalter ausdruecklich auf 1: Bliebe
            // er auf 0, koennte der Fall eine Uebernahme nicht von "war schon 0"
            // unterscheiden.
            int zeuge = ErsteAnlage(PROJEKT_ZWEI_WP, WizardItemClass.PUFFER_TYP);
            Assert.True(zeuge > 0, "Projekt 1019 fuehrt keine zweite Anlagenart als Zeugen.");
            Sql("UPDATE Tab_Energieanlagen SET Heizstab = 1 WHERE ID = ?",
                new DbParam("@z", zeuge));

            Assert.True(HeizstabJeWaermepumpe.ProjektschalterVorhanden());
            Assert.True(HeizstabJeWaermepumpe.UebernahmeNoetig());

            // --- Die Uebernahme ---------------------------------------------------
            DataRepository.ExecuteNonQuery(HeizstabJeWaermepumpe.SqlUebernahme());

            Assert.True(Ja(WP_A));
            Assert.True(Ja(WP_B));
            Assert.False(Ja(WP_OHNE));          // Projekt 1017 stand auf 0
            Assert.True(Ja(zeuge));             // andere Anlagenart: unberuehrt
            Assert.Equal(0L, Zahl(HeizstabJeWaermepumpe.Zaehlung()));
            Assert.False(HeizstabJeWaermepumpe.UebernahmeNoetig());

            // --- Das Entfernen ----------------------------------------------------
            DataRepository.ExecuteNonQuery(HeizstabJeWaermepumpe.SqlSpalteEntfernen());

            Assert.False(HeizstabJeWaermepumpe.ProjektschalterVorhanden());
            Assert.False(DataRepository.SpalteVorhanden(
                HeizstabJeWaermepumpe.TABELLE_EINSTELLUNGEN,
                HeizstabJeWaermepumpe.SPALTE_PROJEKT));

            // Und die Werte an den Anlagen stehen weiter - das Entfernen fasst sie nicht an.
            Assert.True(Ja(WP_A));
            Assert.True(Ja(WP_B));

            // Ohne Spalte ist der Schritt gelaufen: Er meldet "nichts zu tun" statt zu
            // scheitern - genau das macht ihn wiederholbar.
            Assert.False(HeizstabJeWaermepumpe.UebernahmeNoetig());
        }

        /// <summary>
        /// Die Typnummer in den Anweisungen ist die, nach der der LAUF seine Module
        /// sucht — sonst migrierte der Schritt eine andere Menge, als gerechnet wird.
        /// </summary>
        /// <remarks>
        /// Ein <c>const string</c> kann keine Zahl einsetzen; die Nummer steht deshalb
        /// zweimal da (<c>TYP_WAERMEPUMPE</c> und <c>TYP_WAERMEPUMPE_TEXT</c>). Dieser
        /// Fall haelt beide gegeneinander UND gegen
        /// <c>WizardItemClass.WP_TYP</c> — die Nummer, mit der
        /// <c>SimulationControl.WP_Liste_Laden</c> filtert.
        /// </remarks>
        [Fact]
        public void Die_Anweisungen_filtern_nach_der_Typnummer_des_Laufs()
        {
            Assert.Equal(WizardItemClass.WP_TYP, HeizstabJeWaermepumpe.TYP_WAERMEPUMPE);
            Assert.Equal(WizardItemClass.WP_TYP.ToString(),
                         HeizstabJeWaermepumpe.TYP_WAERMEPUMPE_TEXT);

            Assert.Contains("\"ID_Type\" = " + HeizstabJeWaermepumpe.TYP_WAERMEPUMPE_TEXT,
                            HeizstabJeWaermepumpe.SqlUebernahme(), StringComparison.Ordinal);
            Assert.Contains("\"ID_Type\" = " + HeizstabJeWaermepumpe.TYP_WAERMEPUMPE_TEXT,
                            HeizstabJeWaermepumpe.Zaehlung(), StringComparison.Ordinal);

            // Die Reihenfolge der zwei Anweisungen ist tragend: erst uebernehmen, dann
            // entfernen. Umgekehrt waere der Wert weg, bevor er an den Anlagen steht.
            var anweisungen = new System.Collections.Generic.List<string>();
            foreach (var a in HeizstabJeWaermepumpe.Anweisungen) anweisungen.Add(a.Value);
            Assert.Equal(2, anweisungen.Count);
            Assert.Equal(HeizstabJeWaermepumpe.SqlUebernahme(), anweisungen[0]);
            Assert.Equal(HeizstabJeWaermepumpe.SqlSpalteEntfernen(), anweisungen[1]);
        }

        // =================================================================================
        // 2 - Der Rechenweg: der Schalter wirkt JE MODUL
        // =================================================================================

        /// <summary>
        /// Zwei Waermepumpen im SELBEN Projekt, eine mit und eine ohne Heizstab: Nur die
        /// eine bekommt eine Heizstabphase.
        /// </summary>
        /// <remarks>
        /// <para><b>Das ist der Kern des Entscheids.</b> Vor ihm war
        /// <c>SimulationWaermepumpe.Mit_Heizstab</c> EIN Wert fuer alle Module, gesetzt
        /// aus der Projekteinstellung — dieser Fall war schlicht nicht darstellbar.</para>
        ///
        /// <para><b>Gerechnet wird das Modul, nicht das Projekt.</b> Der Fall baut die
        /// Module ueber <c>Vorbereiten_Zweikanalig</c> auf (dort liest
        /// <c>ModuleAufbauen</c> den Schalter aus der Anlagenzeile) und ruft dann EINE
        /// Heizstabphase mit einem offenen Rest, der fuer beide Module reichte. Was
        /// ankommt, steht in <c>Modul_Heizstab</c>.</para>
        /// </remarks>
        [Fact]
        public void Nur_die_Waermepumpe_mit_Heizstab_bekommt_eine_Heizstabphase()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            // A MIT, B OHNE - im selben Projekt und im selben Lauf.
            Sql("UPDATE Tab_Energieanlagen SET Heizstab = 1 WHERE ID = ?", new DbParam("@a", WP_A));
            Sql("UPDATE Tab_Energieanlagen SET Heizstab = 0 WHERE ID = ?", new DbParam("@b", WP_B));

            var wp = new SimulationWaermepumpe();
            wp.wp_list.Add(WP_A);
            wp.wp_list.Add(WP_B);
            wp.Temperatur = new double[Kanalsatz.STUNDEN_JAHR];
            wp.WP_Strombedarf_stuendlich = new double[Kanalsatz.STUNDEN_JAHR];

            Assert.True(wp.Vorbereiten_Zweikanalig(), wp.Fehlertext);
            Assert.Equal(2, wp.wp_model.Count);

            // Ein offener Rest, der fuer BEIDE Heizstaebe reichte (6 + 9 kW).
            double[] rest = Rest(1000.0);
            wp.Heizstabphase(0, rest);

            Assert.True(wp.Modul_Heizstab[0] > 0,
                        "Die Waermepumpe MIT Heizstab hat nicht geheizt.");
            Assert.Equal(0.0, wp.Modul_Heizstab[1]);
            Assert.Equal(wp.Modul_Heizstab[0], wp.HeizstabGesamtKwh, 6);

            // --- Die Gegenprobe: umgekehrt belegt ---------------------------------
            Sql("UPDATE Tab_Energieanlagen SET Heizstab = 0 WHERE ID = ?", new DbParam("@a", WP_A));
            Sql("UPDATE Tab_Energieanlagen SET Heizstab = 1 WHERE ID = ?", new DbParam("@b", WP_B));

            var wp2 = new SimulationWaermepumpe();
            wp2.wp_list.Add(WP_A);
            wp2.wp_list.Add(WP_B);
            wp2.Temperatur = new double[Kanalsatz.STUNDEN_JAHR];
            wp2.WP_Strombedarf_stuendlich = new double[Kanalsatz.STUNDEN_JAHR];

            Assert.True(wp2.Vorbereiten_Zweikanalig(), wp2.Fehlertext);
            wp2.Heizstabphase(0, Rest(1000.0));

            Assert.Equal(0.0, wp2.Modul_Heizstab[0]);
            Assert.True(wp2.Modul_Heizstab[1] > 0);

            // Und OHNE beide steigt die Phase ganz aus - wie frueher der Projektschalter.
            Sql("UPDATE Tab_Energieanlagen SET Heizstab = 0 WHERE ID IN (?, ?)",
                new DbParam("@a", WP_A), new DbParam("@b", WP_B));

            var wp3 = new SimulationWaermepumpe();
            wp3.wp_list.Add(WP_A);
            wp3.wp_list.Add(WP_B);
            wp3.Temperatur = new double[Kanalsatz.STUNDEN_JAHR];
            wp3.WP_Strombedarf_stuendlich = new double[Kanalsatz.STUNDEN_JAHR];

            Assert.True(wp3.Vorbereiten_Zweikanalig(), wp3.Fehlertext);
            wp3.Heizstabphase(0, Rest(1000.0));

            Assert.Equal(0.0, wp3.HeizstabGesamtKwh);
        }

        // =================================================================================
        // Hilfen
        // =================================================================================

        /// <summary>
        /// Legt den Projektschalter wieder an, wenn er fehlt — WORTGLEICH zur
        /// Schemadefinition in <c>sql/schema/001_grundschema.sql</c>, damit der Fall
        /// gegen dieselbe Spalte laeuft, die der Bestand hatte.
        /// </summary>
        private static void ProjektschalterHerstellen()
        {
            if (HeizstabJeWaermepumpe.ProjektschalterVorhanden()) return;

            DataRepository.ExecuteNonQuery(
                "ALTER TABLE \"" + HeizstabJeWaermepumpe.TABELLE_EINSTELLUNGEN +
                "\" ADD COLUMN \"" + HeizstabJeWaermepumpe.SPALTE_PROJEKT +
                "\" INTEGER NOT NULL DEFAULT 0 CHECK (\"" +
                HeizstabJeWaermepumpe.SPALTE_PROJEKT + "\" IN (0,1))");
        }

        /// <summary>Ein offener Bedarf in jedem Kanal [kWh].</summary>
        private static double[] Rest(double wert)
        {
            double[] r = new double[Kanal.ANZAHL];
            for (int k = 0; k < Kanal.ANZAHL; k++) r[k] = wert;
            return r;
        }

        /// <summary>Steht der Heizstabschalter dieser Anlage?</summary>
        private static bool Ja(int idAnlage)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT Heizstab FROM Tab_Energieanlagen WHERE ID = ?",
                new DbParam("@id", idAnlage));
            return v != null && v != DBNull.Value && Convert.ToBoolean(v);
        }

        /// <summary>Die erste Anlage einer Art im Projekt, oder 0.</summary>
        private static int ErsteAnlage(int idProjekt, int idType)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = ? ORDER BY ID",
                new DbParam("@p", idProjekt), new DbParam("@t", idType));
            return (v == null || v == DBNull.Value) ? 0 : Convert.ToInt32(v);
        }

        private static long Zahl(string sql)
        {
            object v = DataRepository.ExecuteScalar(sql);
            return (v == null || v == DBNull.Value) ? -1 : Convert.ToInt64(v);
        }

        private static void Sql(string sql, params DbParam[] p)
        {
            DataRepository.ExecuteNonQuery(sql, p);
        }
    }
}
