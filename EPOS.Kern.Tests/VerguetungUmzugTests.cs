using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die Einspeisevergütung verlässt die Trägerkarte (Anwenderentscheid
    /// <b>SP-E-5 (a)</b> vom 17.09.2026).
    ///
    /// <para>Geprüft wird beides: die neue QUELLENKETTE der Speicherwelt
    /// (<c>StromPreisCtrl.BaueVerguetungen</c> holt v_pv und v_bhkw aus den
    /// Wirtschaftlichkeitsparametern, rechnet €/kWh in ct/kWh um und sagt es, wenn
    /// nichts gepflegt ist) und der DATENSCHRITT 84
    /// (<see cref="VerguetungUmzug"/>), der die gepflegten Kartenwerte hinüberrettet.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class VerguetungUmzugTests
    {
        // Testdatenbank: 1017 trägt an zwei Stromträgern den Kartenwert 5,0/5,0 und
        // hatte VOR Schritt 84 keinen Parametersatz. 1019 trägt denselben Kartenwert
        // und hatte einen Parametersatz mit Einspeiseverguetung = 0 und KWK = NULL.
        // 1030 trägt keinen Kartenwert und einen Parametersatz mit 0.
        private const int PROJEKT_OHNE_SATZ = 1017;
        private const int PROJEKT_MIT_NULLSATZ = 1019;
        private const int PROJEKT_OHNE_KARTENWERT = 1030;

        /// <summary>Der Kartenwert der Testdatenbank [ct/kWh].</summary>
        private const double KARTE_CT = 5.0;

        /// <summary>Derselbe Wert in der Einheit der Parameter [€/kWh].</summary>
        private const double KARTE_EUR = 0.05;

        // =================================================================
        //  Der Datenschritt 84
        // =================================================================

        /// <summary>
        /// Der Regelfall: Ein Projekt mit Kartenwert und OHNE Parametersatz bekommt
        /// einen — mit der Vergütung in €/kWh, sonst leer. Der zweite Lauf ändert
        /// nichts mehr.
        /// </summary>
        [Fact]
        public void Ohne_Parametersatz_legt_der_Schritt_einen_an()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Stand83Herstellen(PROJEKT_OHNE_SATZ, satzLoeschen: true);

            Assert.True(VerguetungUmzug.ZaehlungUmzug() > 0);
            IReadOnlyList<string> protokoll = VerguetungUmzug.Umziehen();
            Assert.Contains(protokoll, z => z.Contains("angelegt"));

            WirtschaftlichkeitParameter p =
                new WirtschaftlichkeitCtrl().LadeParameter(PROJEKT_OHNE_SATZ);
            Assert.Equal(KARTE_EUR, p.Einspeiseverguetung, 9);
            Assert.True(p.EinspeiseverguetungKWK.HasValue);
            Assert.Equal(KARTE_EUR, p.EinspeiseverguetungKWK.Value, 9);

            // Die übrigen Spalten bleiben LEER und damit bei ihrem Vorgabewert -
            // der Satz sagt nichts, was niemand eingetragen hat.
            Assert.Equal(new WirtschaftlichkeitParameter().Zinssatz, p.Zinssatz, 9);
            Assert.Equal(new WirtschaftlichkeitParameter().Betrachtungszeitraum,
                         p.Betrachtungszeitraum);
            Assert.Equal(new WirtschaftlichkeitParameter().KwkgStichtag, p.KwkgStichtag);

            // Wiederholbar: Ein zweiter Lauf findet nichts mehr.
            Assert.Equal(0, VerguetungUmzug.ZaehlungUmzug());
            Assert.Empty(VerguetungUmzug.Umziehen());
        }

        /// <summary>
        /// Eine gepflegte 0 in <c>Einspeiseverguetung</c> heißt „nicht gepflegt": Der
        /// Kartenwert gewinnt, sonst verlöre genau das Projekt seine Zahl, das schon
        /// einen Parametersatz hat.
        /// </summary>
        [Fact]
        public void Eine_gepflegte_Null_gilt_als_nicht_gepflegt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Stand83Herstellen(PROJEKT_MIT_NULLSATZ, satzLoeschen: false);

            Assert.True(VerguetungUmzug.ZaehlungUmzug() > 0);
            Assert.Contains(VerguetungUmzug.Umziehen(), z => z.Contains("ergaenzt"));

            WirtschaftlichkeitParameter p =
                new WirtschaftlichkeitCtrl().LadeParameter(PROJEKT_MIT_NULLSATZ);
            Assert.Equal(KARTE_EUR, p.Einspeiseverguetung, 9);
            Assert.Equal(KARTE_EUR, p.EinspeiseverguetungKWK.Value, 9);
        }

        /// <summary>
        /// Ein gepflegter Parameter GEWINNT — er ist die jüngere und die fachlich
        /// führende Angabe. Der Schritt schreibt nichts darüber.
        /// </summary>
        [Fact]
        public void Ein_gepflegter_Parameter_gewinnt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Stand83Herstellen(PROJEKT_MIT_NULLSATZ, satzLoeschen: false);
            ParameterSetzen(PROJEKT_MIT_NULLSATZ, 0.0912, 0.1234);

            VerguetungUmzug.Umziehen();

            WirtschaftlichkeitParameter p =
                new WirtschaftlichkeitCtrl().LadeParameter(PROJEKT_MIT_NULLSATZ);
            Assert.Equal(0.0912, p.Einspeiseverguetung, 9);
            Assert.Equal(0.1234, p.EinspeiseverguetungKWK.Value, 9);
        }

        /// <summary>
        /// Ohne Kartenwert wird nichts angefasst: 1030 trägt keinen, sein
        /// Parametersatz bleibt bei 0.
        /// </summary>
        [Fact]
        public void Ohne_Kartenwert_bleibt_der_Parametersatz_wie_er_ist()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ParameterSetzen(PROJEKT_OHNE_KARTENWERT, 0.0, null);
            VerguetungUmzug.Umziehen();

            WirtschaftlichkeitParameter p =
                new WirtschaftlichkeitCtrl().LadeParameter(PROJEKT_OHNE_KARTENWERT);
            Assert.Equal(0.0, p.Einspeiseverguetung, 9);
            Assert.Null(p.EinspeiseverguetungKWK);
        }

        /// <summary>
        /// Auf dem Zielstand (Schritt 85 hat die Kartenspalten entfernt) ist der Schritt
        /// STILL: nichts zu zählen, nichts umzuziehen — und keine Datenbankmeldung. Der
        /// Engine-Modus sammelt jede Meldung von <c>DataRepository.FehlerMelden</c>, statt
        /// sie zu zeigen; die Sammlung muss leer bleiben. Ohne die Prüfung in
        /// <c>Zeilen()</c> stünde hier „no such column: eps.Verguetung_PV" — bei jeder
        /// Arbeitskopie, in jedem Protokoll.
        /// </summary>
        [Fact]
        public void Ohne_Kartenspalten_schweigt_der_Schritt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.False(VerguetungUmzug.KartenspaltenVorhanden());

            using (DataRepository.EngineModus())
            {
                Assert.Equal(0, VerguetungUmzug.ZaehlungUmzug());
                Assert.Empty(VerguetungUmzug.Umziehen());
                Assert.Empty(DataRepository.StilleFehlerAbholen());
            }
        }

        // =================================================================
        //  Die Quellenkette der Speicherwelt
        // =================================================================

        /// <summary>
        /// v_pv und v_bhkw kommen aus den Parametern, umgerechnet von €/kWh in
        /// ct/kWh. 5 ct/kWh der alten Karte sind 0,05 €/kWh der Parameter sind
        /// wieder 5 ct/kWh — genau deshalb bleibt der Referenzlauf byte-gleich.
        /// </summary>
        [Fact]
        public void Die_Verguetung_kommt_aus_den_Parametern_in_ct_je_kWh()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ParameterSetzen(PROJEKT_OHNE_SATZ, KARTE_EUR, KARTE_EUR);

            StromVerguetungsErgebnis e =
                new StromPreisCtrl().BaueVerguetungen(PROJEKT_OHNE_SATZ, 4);

            Assert.Equal(KARTE_CT, e.PvCtKwh[0], 9);
            Assert.Equal(KARTE_CT, e.BhkwCtKwh[0], 9);
            Assert.DoesNotContain("0 ct/kWh", e.Hinweis);
        }

        /// <summary>
        /// Der KWK-Satz führt v_bhkw; fehlt er, gilt der allgemeine Satz. v_pv bleibt
        /// in beiden Fällen der allgemeine Satz.
        /// </summary>
        [Fact]
        public void Der_KWK_Satz_fuehrt_v_bhkw_sonst_gilt_der_allgemeine()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ParameterSetzen(PROJEKT_OHNE_SATZ, 0.08, 0.12);
            StromVerguetungsErgebnis mitKwk =
                new StromPreisCtrl().BaueVerguetungen(PROJEKT_OHNE_SATZ, 2);
            Assert.Equal(8.0, mitKwk.PvCtKwh[0], 9);
            Assert.Equal(12.0, mitKwk.BhkwCtKwh[0], 9);

            ParameterSetzen(PROJEKT_OHNE_SATZ, 0.08, null);
            StromVerguetungsErgebnis ohneKwk =
                new StromPreisCtrl().BaueVerguetungen(PROJEKT_OHNE_SATZ, 2);
            Assert.Equal(8.0, ohneKwk.PvCtKwh[0], 9);
            Assert.Equal(8.0, ohneKwk.BhkwCtKwh[0], 9);
        }

        /// <summary>
        /// <b>Kein stiller 5-ct-Rückfall mehr.</b> Ohne gepflegten Wert rechnet der
        /// Lauf mit 0 und SAGT es — in beiden Sprachen, denn das Protokoll geht in
        /// den Bericht.
        /// </summary>
        [Theory]
        [InlineData("de-DE", "Wirtschaftlichkeitsparametern")]
        [InlineData("en-US", "economic parameters")]
        public void Ohne_gepflegte_Verguetung_null_und_benannter_Hinweis(string kultur,
                                                                        string erwartet)
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ParameterSetzen(PROJEKT_OHNE_SATZ, 0.0, null);

            CultureInfo vorher = CultureInfo.CurrentUICulture;
            CultureInfo katalogVorher = Resource.Culture;
            try
            {
                CultureInfo k = new CultureInfo(kultur);
                Thread.CurrentThread.CurrentUICulture = k;
                CultureInfo.CurrentUICulture = k;
                Resource.Culture = k;

                StromVerguetungsErgebnis e =
                    new StromPreisCtrl().BaueVerguetungen(PROJEKT_OHNE_SATZ, 2);

                Assert.Equal(0.0, e.PvCtKwh[0], 9);
                Assert.Equal(0.0, e.BhkwCtKwh[0], 9);
                Assert.Contains(erwartet, e.Hinweis);
            }
            finally
            {
                Resource.Culture = katalogVorher;
                Thread.CurrentThread.CurrentUICulture = vorher;
                CultureInfo.CurrentUICulture = vorher;
            }
        }

        /// <summary>
        /// Der Rückfallwert der Speicherparameter steht auf 0 — nicht mehr auf
        /// 5 ct/kWh. Auch der Weg über <c>StandardParameter</c> erfindet damit keine
        /// Vergütung mehr (SpeicherFlottenStudieCtrl, Dashboard-Kachel).
        /// </summary>
        [Fact]
        public void Der_Rueckfallwert_der_Speicherparameter_ist_null()
        {
            Assert.Equal(0.0, StromspeicherSimCtrl.VERGUETUNG_OHNE_PFLEGE_CT_KWH);
            Assert.Equal(0.0, StromspeicherSimCtrl.StandardParameter(10.0, 5.0).VerguetungCtKwh);
        }

        // =================================================================
        //  Hilfsmittel
        // =================================================================

        /// <summary>
        /// Stellt auf der Arbeitskopie den Stand VOR Schritt 84 her: Kartenwerte
        /// gesetzt, Parametersatz leer bzw. ganz fort.
        /// </summary>
        private static void Stand83Herstellen(int projekt, bool satzLoeschen)
        {
            // Die Kartenspalten sind mit Schemaschritt 85 entfallen. Der Stand VOR
            // Schritt 84 hatte sie - fuer diese Arbeitskopie kommen sie deshalb
            // zurueck; sie verschwindet mit dem Prueflauf.
            TestDatenbank.AltspaltenStrompreisWiederherstellen();

            DataRepository.ExecuteNonQuery(
                "UPDATE [" + SchemaKatalog.ENERGY_PROJECT_SETTINGS + "] SET [" +
                StrompreisAltspalten.SPALTE_VERGUETUNG_PV + "] = ?, [" +
                StrompreisAltspalten.SPALTE_VERGUETUNG_BHKW + "] = ? WHERE ID_Projekt = ?",
                new DbParam("@pv", DbParamTyp.Double) { Wert = KARTE_CT },
                new DbParam("@bh", DbParamTyp.Double) { Wert = KARTE_CT },
                new DbParam("@p", DbParamTyp.Integer) { Wert = projekt });

            if (satzLoeschen)
                DataRepository.ExecuteNonQuery(
                    "DELETE FROM [" + WirtschaftlichkeitCtrl.TAB_PARAMETER + "] WHERE ID_Projekt = ?",
                    new DbParam("@p", DbParamTyp.Integer) { Wert = projekt });
            else
                ParameterSetzen(projekt, 0.0, null);
        }

        /// <summary>
        /// Setzt die beiden Vergütungsfelder des Parametersatzes; fehlt der Satz, legt
        /// dieser Weg ihn über den Controller an.
        /// </summary>
        private static void ParameterSetzen(int projekt, double evEurKwh, double? evKwkEurKwh)
        {
            WirtschaftlichkeitCtrl ctrl = new WirtschaftlichkeitCtrl();
            WirtschaftlichkeitParameter p = ctrl.LadeParameter(projekt);
            p.IdStamm = projekt;
            p.Einspeiseverguetung = evEurKwh;
            p.EinspeiseverguetungKWK = evKwkEurKwh;
            ctrl.SpeichereParameter(p);
        }
    }
}
