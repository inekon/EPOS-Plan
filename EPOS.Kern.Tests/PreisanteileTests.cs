using System;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Ein Bauplan für beide Träger</b> — die Preiszerlegung des Brennstoffs und
    /// die des Stroms lesen, schreiben und rechnen nach derselben Regel
    /// (Anwenderentscheid 17.09.2026 auf die Fachfrage <c>Anteil_Modus</c>).
    ///
    /// <para>Geprüft wird dreierlei: dass der Brennstoffblock den Modus weder liest
    /// noch schreibt, dass ein Bestandssatz mit <c>Anteil_Modus = 'Aufgeschluesselt'</c>
    /// sich genau wie jeder andere liest, und dass Summe und Rest bei BEIDEN Trägern
    /// aus derselben Stelle kommen (<see cref="Preisanteile"/>).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class PreisanteileTests
    {
        // Testdatenbank: 1030 führt einen Gas-Träger (63) mit Anteil_Modus
        // „Gesamtwert"; 1045 führt denselben Träger.
        private const int PROJEKT = 1030;
        private const int TRAEGER_GAS = 63;

        // =================================================================
        //  Summe und Rest kommen aus EINER Stelle
        // =================================================================

        /// <summary>
        /// Der Brennstofffall: Die Summe der aktiven Anteile und der Rest gegen den
        /// Arbeitspreis kommen aus <see cref="Preisanteile"/> — nicht aus einer
        /// eigenen Rechnung dieses Trägers.
        /// </summary>
        [Fact]
        public void Summe_und_Rest_des_Brennstoffs_kommen_aus_dem_gemeinsamen_Bauplan()
        {
            BrennstoffBestandteilModel m = new BrennstoffBestandteilModel
            {
                Energiesteuer = 0.55, Energiesteuer_Aktiv = true,
                CO2 = 1.10, CO2_Aktiv = true,
                Netzentgelt = 1.40, Netzentgelt_Aktiv = false,   // gepflegt, aber aus
                Vertrieb = null, Vertrieb_Aktiv = false
            };

            Preiszerlegung satz = BrennstoffBestandteilCtrl.AlsPreiszerlegung(m);

            Assert.Equal(1.65, Preisanteile.SummeCtKwh(satz), 9);
            Assert.Equal(satz.SummeAktivCtKwh, Preisanteile.SummeCtKwh(satz), 12);

            // 6,44 − 1,65 = 4,79; ein negativer Rest wird NICHT bei 0 abgeschnitten.
            Assert.Equal(4.79, Preisanteile.RestCtKwh(6.44, satz), 9);
            Assert.Equal(-0.65, Preisanteile.RestCtKwh(1.00, satz), 9);
        }

        /// <summary>
        /// Derselbe Fall für den Strom — dieselben zwei Aufrufe, dieselbe Regel: Was
        /// inaktiv ist, trägt nichts bei, und der Rest ist der Abstand zum
        /// Arbeitspreis.
        /// </summary>
        [Fact]
        public void Summe_und_Rest_des_Stroms_kommen_aus_demselben_Bauplan()
        {
            StrompreisZerlegungModel m = new StrompreisZerlegungModel
            {
                Beschaffung = 26.254, Beschaffung_Aktiv = true,
                Vertrieb = 2.0, Vertrieb_Aktiv = true,
                Netzentgelt = 7.5, Netzentgelt_Aktiv = true,
                Stromsteuer = 2.05, Stromsteuer_Aktiv = false,   // gepflegt, aber aus
                Konzession = 1.32, Konzession_Aktiv = false,
                Umlagen = 1.2, Umlagen_Aktiv = false
            };

            Preiszerlegung satz = StrompreisZerlegungCtrl.AlsPreiszerlegung(m);

            Assert.Equal(35.754, Preisanteile.SummeCtKwh(satz), 9);
            Assert.Equal(satz.SummeAktivCtKwh, Preisanteile.SummeCtKwh(satz), 12);

            Assert.Equal(4.246, Preisanteile.RestCtKwh(40.0, satz), 9);
            Assert.Equal(-5.754, Preisanteile.RestCtKwh(30.0, satz), 9);
        }

        /// <summary>
        /// Die eine Rechnung, zwei Träger: Tragen beide Sätze dieselben aktiven
        /// Beträge, stehen Summe und Rest auf derselben Zahl. Das ist die Zusage des
        /// gemeinsamen Bauplans — und der Hebel der Gegenprobe: Rechnet
        /// <see cref="Preisanteile"/> falsch, fallen BEIDE Trägerfälle.
        /// </summary>
        [Fact]
        public void Gleiche_Betraege_ergeben_bei_beiden_Traegern_dieselbe_Summe_und_denselben_Rest()
        {
            BrennstoffBestandteilModel b = new BrennstoffBestandteilModel
            {
                Energiesteuer = 2.0, Energiesteuer_Aktiv = true,
                CO2 = 3.0, CO2_Aktiv = true
            };

            StrompreisZerlegungModel s = new StrompreisZerlegungModel
            {
                Beschaffung = 2.0, Beschaffung_Aktiv = true,
                Vertrieb = 3.0, Vertrieb_Aktiv = true,
                Netzentgelt = 7.5, Netzentgelt_Aktiv = false,
                Stromsteuer = 2.05, Stromsteuer_Aktiv = false,
                Konzession = 1.32, Konzession_Aktiv = false,
                Umlagen = 1.2, Umlagen_Aktiv = false
            };

            Preiszerlegung satzB = BrennstoffBestandteilCtrl.AlsPreiszerlegung(b);
            Preiszerlegung satzS = StrompreisZerlegungCtrl.AlsPreiszerlegung(s);

            Assert.Equal(Preisanteile.SummeCtKwh(satzB), Preisanteile.SummeCtKwh(satzS), 12);
            Assert.Equal(Preisanteile.RestCtKwh(8.0, satzB),
                         Preisanteile.RestCtKwh(8.0, satzS), 12);
            Assert.Equal(5.0, Preisanteile.SummeCtKwh(satzB), 9);
            Assert.Equal(3.0, Preisanteile.RestCtKwh(8.0, satzS), 9);
        }

        /// <summary>
        /// Das SET-Fragment der Schreibseite ist für beide Träger dasselbe: Wertspalte
        /// und Aktiv-Spalte, je ein <c>?</c>-Parameter, kein zusammengesetzter
        /// SQL-Text.
        /// </summary>
        [Fact]
        public void Das_SET_Fragment_ist_fuer_beide_Traeger_dasselbe()
        {
            Assert.Equal("[Anteil_CO2] = ?, [Anteil_CO2_Aktiv] = ?, ",
                         Preisanteile.SetzPaar(SchemaKatalog.SPALTE_BB_CO2));
            Assert.Equal("[Aufschlag_Netzentgelt] = ?, [Aufschlag_Netzentgelt_Aktiv] = ?, ",
                         Preisanteile.SetzPaar(SchemaKatalog.SPALTE_AUFSCHLAG_NETZENTGELT));
        }

        /// <summary>
        /// <c>null</c> geht als <c>DBNull</c> in die Datenbank, nicht als 0 — der
        /// Unterschied zwischen „kein Anteil erfasst" und „der Anteil ist null".
        /// </summary>
        [Fact]
        public void Ein_nicht_erfasster_Anteil_wird_DBNull_und_nicht_null_Komma_null()
        {
            Assert.Equal(DBNull.Value, Preisanteile.Wert("@x", null).Wert);
            Assert.Equal(1.25, Preisanteile.Wert("@x", 1.25).Wert);
            Assert.Equal(0.0, Preisanteile.Wert("@x", 0.0).Wert);
        }

        // =================================================================
        //  Der Brennstoff kennt keinen Modus mehr
        // =================================================================

        /// <summary>
        /// Das Modell führt keinen Modus mehr — weder als Feld noch als Vorgabe.
        /// </summary>
        [Fact]
        public void Das_Brennstoffmodell_fuehrt_kein_Modusfeld_mehr()
        {
            Assert.Null(typeof(BrennstoffBestandteilModel).GetField("Modus"));
            Assert.Null(typeof(BrennstoffBestandteilModel).GetProperty("Modus"));
        }

        /// <summary>
        /// Lesen und Schreiben ohne Modus: Die vier Anteile gehen unverändert hin und
        /// zurück, <c>null</c> bleibt <c>null</c>, und ein aktiver Schalter ohne Wert
        /// bleibt aktiv.
        /// </summary>
        [Fact]
        public void Die_Brennstoffanteile_gehen_ohne_Modus_hin_und_zurueck()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            BrennstoffBestandteilCtrl ctrl = new BrennstoffBestandteilCtrl();

            BrennstoffBestandteilModel m = new BrennstoffBestandteilModel
            {
                ID_Projekt = PROJEKT, ID_Energietraeger = TRAEGER_GAS,
                Energiesteuer = 0.55, Energiesteuer_Aktiv = true,
                CO2 = 1.10, CO2_Aktiv = true,
                Netzentgelt = null, Netzentgelt_Aktiv = false,
                Vertrieb = null, Vertrieb_Aktiv = true        // aktiv ohne Wert: trägt 0
            };

            Assert.True(ctrl.Update(m));

            BrennstoffBestandteilModel neu = ctrl.Read(PROJEKT, TRAEGER_GAS);

            Assert.True(neu.AusDatenbank);
            Assert.Equal(0.55, neu.Energiesteuer.Value, 9);
            Assert.True(neu.Energiesteuer_Aktiv);
            Assert.Equal(1.10, neu.CO2.Value, 9);
            Assert.Null(neu.Netzentgelt);
            Assert.False(neu.Netzentgelt_Aktiv);
            Assert.Null(neu.Vertrieb);
            Assert.True(neu.Vertrieb_Aktiv);

            Assert.Equal(1.65,
                Preisanteile.SummeCtKwh(BrennstoffBestandteilCtrl.AlsPreiszerlegung(neu)), 9);
        }

        /// <summary>
        /// <b>Der Bestandssatz.</b> Eine Zeile, in der <c>Anteil_Modus</c> noch auf
        /// „Aufgeschluesselt" steht, liest sich genau wie eine auf „Gesamtwert" — die
        /// Spalte hat keine Wirkung mehr. Und ein Schreibzugriff lässt sie stehen,
        /// statt sie zu pflegen.
        /// </summary>
        [Fact]
        public void Ein_Bestandssatz_mit_altem_Modus_liest_sich_gleich()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            BrennstoffBestandteilCtrl ctrl = new BrennstoffBestandteilCtrl();

            BrennstoffBestandteilModel m = new BrennstoffBestandteilModel
            {
                ID_Projekt = PROJEKT, ID_Energietraeger = TRAEGER_GAS,
                Energiesteuer = 0.55, Energiesteuer_Aktiv = true,
                CO2 = 1.10, CO2_Aktiv = true
            };
            Assert.True(ctrl.Update(m));

            ModusSetzen(DbWerte.SP_AUFSCHLAG_MODUS_GESAMTWERT);
            BrennstoffBestandteilModel a = ctrl.Read(PROJEKT, TRAEGER_GAS);

            ModusSetzen(DbWerte.SP_AUFSCHLAG_MODUS_AUFGESCHLUESSELT);
            BrennstoffBestandteilModel b = ctrl.Read(PROJEKT, TRAEGER_GAS);

            Assert.Equal(a.Energiesteuer, b.Energiesteuer);
            Assert.Equal(a.Energiesteuer_Aktiv, b.Energiesteuer_Aktiv);
            Assert.Equal(a.CO2, b.CO2);
            Assert.Equal(a.CO2_Aktiv, b.CO2_Aktiv);
            Assert.Equal(a.Netzentgelt, b.Netzentgelt);
            Assert.Equal(a.Vertrieb, b.Vertrieb);
            Assert.Equal(Preisanteile.SummeCtKwh(BrennstoffBestandteilCtrl.AlsPreiszerlegung(a)),
                         Preisanteile.SummeCtKwh(BrennstoffBestandteilCtrl.AlsPreiszerlegung(b)), 12);

            // Der Schreibweg fasst die Spalte nicht an: Sie steht danach unverändert da.
            m.CO2 = 2.20;
            Assert.True(ctrl.Update(m));
            Assert.Equal(DbWerte.SP_AUFSCHLAG_MODUS_AUFGESCHLUESSELT, ModusLesen());
        }

        // =================================================================
        //  Rest 1: der Text nennt die Knöpfe, die es gibt
        // =================================================================

        /// <summary>
        /// <b>Der Hinweis an einer Anlage ohne eigene Positionen</b> nennt die Knöpfe
        /// der heutigen Erzeugerdialoge. Einen Knopf „Kosten bearbeiten…" gibt es dort
        /// nicht mehr; er darf auch in keiner der beiden Sprachen mehr genannt werden.
        /// </summary>
        [Theory]
        [InlineData("de-DE", "Investitionskosten", "Betriebskosten", "Kosten bearbeiten")]
        [InlineData("en-US", "Investment costs", "Operating costs", "Edit costs")]
        public void Der_Hinweis_ohne_Positionen_nennt_die_heutigen_Knoepfe(
            string kultur, string ersterKnopf, string zweiterKnopf, string alterKnopf)
        {
            using var _ = new Kulturvorrichtung(kultur);

            string text = WindowsFormsApplication1.MyResource.Resource.BK_KOSTEN_ANLAGE_OHNE_POSITIONEN;

            Assert.Contains(ersterKnopf, text);
            Assert.Contains(zweiterKnopf, text);
            Assert.DoesNotContain(alterKnopf, text);
        }

        // =================================================================
        //  Hilfsmittel
        // =================================================================

        private static void ModusSetzen(string modus)
        {
            DataRepository.ExecuteNonQuery(
                "UPDATE energy_project_settings SET Anteil_Modus = ? " +
                "WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                new DbParam("@m", DbParamTyp.VarWChar) { Wert = modus },
                new DbParam("@p", DbParamTyp.Integer) { Wert = PROJEKT },
                new DbParam("@t", DbParamTyp.Integer) { Wert = TRAEGER_GAS });
        }

        private static string ModusLesen()
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT Anteil_Modus FROM energy_project_settings " +
                "WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                new DbParam("@p", DbParamTyp.Integer) { Wert = PROJEKT },
                new DbParam("@t", DbParamTyp.Integer) { Wert = TRAEGER_GAS });

            return (v == null || v == DBNull.Value) ? "" : v.ToString();
        }
    }
}
