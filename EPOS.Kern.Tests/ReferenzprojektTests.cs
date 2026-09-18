using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>KONZEPT § 2.9 — das wählbare Vergleichsprojekt.</b> Die Differenzrechnung lief
    /// fest gegen das Stammprojekt; DIN EN 17463 verlangt den Vergleich gegen die
    /// <b>Unterlassensalternative</b>, und welche das ist, entscheidet die Bewertung.
    ///
    /// <para><b>Was hier festgehalten wird.</b> Fünf Dinge:
    /// <list type="number">
    ///   <item><description>Der Schemaschritt 92 steht, und die Spalte ist im ganzen
    ///     Bestand leer — NULL heißt Stamm.</description></item>
    ///   <item><description>Die Wahl übersteht Speichern und Laden; 0 geht als NULL in
    ///     die Datenbank.</description></item>
    ///   <item><description><see cref="Referenzwahl"/> löst auf und fällt BENANNT auf
    ///     den Stamm zurück, wenn die gewählte Referenz fehlt.</description></item>
    ///   <item><description>Referenz = Stamm rechnet <b>bitgleich</b> zum Bestand; eine
    ///     gewählte Variante lenkt alle Differenzkennzahlen um, und sie selbst bekommt
    ///     keine.</description></item>
    ///   <item><description>Die Zeilendefinition kennzeichnet die gewählte Referenz —
    ///     nicht mehr den Stamm.</description></item>
    /// </list></para>
    ///
    /// <para><c>[Collection("Testdatenbank")]</c>, weil
    /// <see cref="DataRepository.PfadUeberschreibung"/> prozessweiter Zustand ist; die
    /// Kultur ist gepinnt, weil die Rückfallwarnung aus den Satellitenressourcen kommt.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class ReferenzprojektTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        /// <summary>Der Stamm der Prüfgruppe — ein Projekt der Testdatenbank.</summary>
        private const int STAMM = 1040;

        /// <summary>Die zwei Varianten der Prüfgruppe. Ihre Ids sind Projekte der
        /// Testdatenbank; gemessen wird die Differenzrechnung, nicht ihr Inhalt.</summary>
        private const int VARIANTE_A = 1041;
        private const int VARIANTE_B = 1042;

        private static readonly CultureInfo DE = CultureInfo.GetCultureInfo("de-DE");

        // =================================================================
        // 1 — Schemaschritt 92
        // =================================================================

        /// <summary>
        /// Der Zielstand trägt den Schritt 92, und die eine Spalte hängt an
        /// <c>Tab_ProjektWirtschaftlichkeit</c>. Die Liste ist die EINE Quelle, aus der
        /// sich Migration, Werkzeug und Testdatenbank bedienen.
        /// </summary>
        [Fact]
        public void Der_Zielstand_traegt_den_Schritt_92()
        {
            Assert.True(SchemaStand.Zielversion >= 92,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter 92.");
            Assert.Single(SchemaKatalog.Schritt92_Referenzprojekt);
            Assert.Equal(SchemaKatalog.TAB_PROJEKTWIRTSCHAFT,
                         SchemaKatalog.Schritt92_Referenzprojekt[0].Tabelle);
            Assert.Equal(SchemaKatalog.SPALTE_PW_REFERENZPROJEKT,
                         SchemaKatalog.Schritt92_Referenzprojekt[0].Name);
            Assert.Equal("LONG", SchemaKatalog.Schritt92_Referenzprojekt[0].TypDefinition);
        }

        /// <summary>Die Testdatenbank führt die Spalte, und sie ist im ganzen Bestand
        /// leer — der Schritt schreibt keinen Wert. Das ist die Ergebnisneutralität.</summary>
        [Fact]
        public void Die_Testdatenbank_fuehrt_die_leere_Spalte()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.True(DataRepository.SpalteVorhanden(SchemaKatalog.TAB_PROJEKTWIRTSCHAFT,
                                                       SchemaKatalog.SPALTE_PW_REFERENZPROJEKT));
            object gepflegt = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM " + SchemaKatalog.TAB_PROJEKTWIRTSCHAFT +
                " WHERE [" + SchemaKatalog.SPALTE_PW_REFERENZPROJEKT + "] IS NOT NULL");
            Assert.Equal(0, Convert.ToInt32(gepflegt));
        }

        // =================================================================
        // 2 — Der Parametersatz
        // =================================================================

        /// <summary>Ohne gepflegte Wahl gilt der Stamm — und der steht als 0 da, nicht
        /// als Verweis auf sich selbst.</summary>
        [Fact]
        public void Ohne_gepflegte_Wahl_gilt_der_Stamm()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Equal(0, new WirtschaftlichkeitCtrl().LadeParameter(STAMM).IdReferenzprojekt);
        }

        /// <summary>
        /// Die Wahl übersteht Speichern und Laden, und die Abwahl räumt die Zelle
        /// wieder LEER — eine geschriebene 0 wäre ein Verweis auf ein Projekt, das es
        /// nicht gibt.
        /// </summary>
        [Fact]
        public void Die_Wahl_ueberlebt_Speichern_und_Laden()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var ctrl = new WirtschaftlichkeitCtrl();
            WirtschaftlichkeitParameter p = ctrl.LadeParameter(STAMM);
            p.IdReferenzprojekt = VARIANTE_A;
            Assert.True(ctrl.SpeichereParameter(p));

            Assert.Equal(VARIANTE_A,
                         new WirtschaftlichkeitCtrl().LadeParameter(STAMM).IdReferenzprojekt);

            p.IdReferenzprojekt = 0;
            Assert.True(ctrl.SpeichereParameter(p));
            Assert.Equal(0, new WirtschaftlichkeitCtrl().LadeParameter(STAMM).IdReferenzprojekt);

            object leer = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM " + SchemaKatalog.TAB_PROJEKTWIRTSCHAFT +
                " WHERE ID_Projekt = ? AND [" + SchemaKatalog.SPALTE_PW_REFERENZPROJEKT +
                "] IS NOT NULL", new DbParam("@p", STAMM));
            Assert.Equal(0, Convert.ToInt32(leer));
        }

        // =================================================================
        // 3 — Die Auflösung (ohne Datenbank)
        // =================================================================

        /// <summary>Ohne Wahl ist die Referenz der Stamm, und niemand fällt zurück.</summary>
        [Fact]
        public void Ohne_Wahl_ist_die_Referenz_der_Stamm()
        {
            Referenzwahl w = Referenzwahl.Bestimme(Gruppe(), STAMM, 0);

            Assert.Equal(STAMM, w.IdReferenz);
            Assert.False(w.Rueckfall);
            Assert.Null(w.Warnung);
            Assert.True(w.IstReferenz(STAMM));
            Assert.False(w.IstReferenz(VARIANTE_A));
        }

        /// <summary>Eine gewählte Variante der Gruppe wird die Referenz.</summary>
        [Fact]
        public void Eine_gewaehlte_Variante_wird_die_Referenz()
        {
            Referenzwahl w = Referenzwahl.Bestimme(Gruppe(), STAMM, VARIANTE_A);

            Assert.Equal(VARIANTE_A, w.IdReferenz);
            Assert.False(w.Rueckfall);
            Assert.Null(w.Warnung);
            Assert.Equal("Variante A", w.Anzeige);
        }

        /// <summary>
        /// RANDFALL (§ 2.9): Eine gelöschte oder nicht mehr zur Gruppe gehörende
        /// Referenz fällt auf den Stamm zurück — <b>mit Warnzeile</b>. Ein stiller
        /// Rückfall wäre eine andere Rechnung ohne Auskunft.
        /// </summary>
        [Fact]
        public void Eine_fehlende_Referenz_faellt_benannt_auf_den_Stamm_zurueck()
        {
            Referenzwahl w = Referenzwahl.Bestimme(Gruppe(), STAMM, 999999);

            Assert.Equal(STAMM, w.IdReferenz);
            Assert.True(w.Rueckfall);
            Assert.False(string.IsNullOrEmpty(w.Warnung));
            Assert.Contains("Stammprojekt", w.Warnung, StringComparison.Ordinal);
        }

        /// <summary>Die Nachweiszeilen nennen die Referenz beim Namen — in beiden
        /// Sichten, und in Sicht 2 beide Referenzen.</summary>
        [Fact]
        public void Die_Nachweiszeilen_nennen_die_Referenz_beim_Namen()
        {
            string sicht1 = Referenzwahl.Nachweiszeile("Stammprojekt");
            Assert.Contains("Stammprojekt", sicht1, StringComparison.Ordinal);

            string sicht2 = Referenzwahl.Nachweiszeile("Variante A", "Stammprojekt");
            Assert.Contains("Variante A", sicht2, StringComparison.Ordinal);
            Assert.Contains("Stammprojekt", sicht2, StringComparison.Ordinal);

            string bericht = Referenzwahl.Deklarationszeile("Variante A", "Stammprojekt");
            Assert.Contains("Variante A", bericht, StringComparison.Ordinal);
            Assert.Contains("Stammprojekt", bericht, StringComparison.Ordinal);

            Assert.Contains("Stammprojekt", Referenzwahl.ValeriDeklaration("Stammprojekt"),
                            StringComparison.Ordinal);
            Assert.Contains("Variante A", Referenzwahl.ReferenzFehlt("Variante A", "kein Ergebnis"),
                            StringComparison.Ordinal);
        }

        // =================================================================
        // 4 — Die Rechnung
        // =================================================================

        /// <summary>
        /// DIE REGRESSIONSZUSAGE. Referenz = Stamm (die Vorgabe) rechnet <b>bitgleich</b>
        /// zum Bestand: Die ausdrückliche Wahl des Stammes und gar keine Wahl liefern
        /// dieselben Zahlen — Kapitalwert, Differenz, Annuität, Amortisation und
        /// interner Zinsfuß.
        /// </summary>
        [Fact]
        public void Referenz_gleich_Stamm_rechnet_bitgleich_zum_Bestand()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            List<WirtschaftlichkeitErgebnis> ohne = Rechne(0);
            List<WirtschaftlichkeitErgebnis> mit = Rechne(STAMM);

            foreach (int id in new[] { STAMM, VARIANTE_A, VARIANTE_B })
            {
                WirtschaftlichkeitErgebnis a = Erwartet(ohne, id);
                WirtschaftlichkeitErgebnis b = Erwartet(mit, id);
                Assert.Equal(a.Kapitalwert, b.Kapitalwert);
                Assert.Equal(a.KapitalwertDiff, b.KapitalwertDiff);
                Assert.Equal(a.AnnuitaetKW, b.AnnuitaetKW);
                Assert.Equal(a.AmortisationJahre, b.AmortisationJahre);
                Assert.Equal(a.IRR, b.IRR);
            }
        }

        /// <summary>
        /// Mit Referenz = Variante A rechnen alle Differenzen gegen A, und <b>A selbst
        /// bekommt keine</b> — so wie zuvor der Stamm keine bekam. Der Stamm dagegen
        /// bekommt jetzt eine.
        /// </summary>
        [Fact]
        public void Mit_gewaehlter_Referenz_rechnen_die_Differenzen_gegen_sie()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            List<WirtschaftlichkeitErgebnis> lauf = Rechne(VARIANTE_A);

            WirtschaftlichkeitErgebnis a = Erwartet(lauf, VARIANTE_A);
            WirtschaftlichkeitErgebnis b = Erwartet(lauf, VARIANTE_B);
            WirtschaftlichkeitErgebnis stamm = Erwartet(lauf, STAMM);

            Assert.False(a.KapitalwertDiff.HasValue);
            Assert.False(a.AnnuitaetKW.HasValue);
            Assert.False(a.IRR.HasValue);

            Assert.True(b.KapitalwertDiff.HasValue);
            Assert.Equal(b.Kapitalwert.Value - a.Kapitalwert.Value, b.KapitalwertDiff.Value, 6);

            Assert.True(stamm.KapitalwertDiff.HasValue);
            Assert.Equal(stamm.Kapitalwert.Value - a.Kapitalwert.Value,
                         stamm.KapitalwertDiff.Value, 6);
        }

        /// <summary>
        /// ABNAHME § 2.15: Die Kapitalwertdifferenz ist linear —
        /// <c>Δ_A(B) = Δ_Stamm(B) − Δ_Stamm(A)</c> auf 0,01 €. Das ist die Probe, dass
        /// die zweite Sicht keine zweite Rechnung ist, sondern dieselbe mit anderer
        /// Referenz.
        /// </summary>
        [Fact]
        public void Die_Kapitalwertdifferenz_ist_linear_in_der_Referenz()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            List<WirtschaftlichkeitErgebnis> gegenStamm = Rechne(0);
            List<WirtschaftlichkeitErgebnis> gegenA = Rechne(VARIANTE_A);

            double diffB = Erwartet(gegenStamm, VARIANTE_B).KapitalwertDiff.Value;
            double diffA = Erwartet(gegenStamm, VARIANTE_A).KapitalwertDiff.Value;
            double paar = Erwartet(gegenA, VARIANTE_B).KapitalwertDiff.Value;

            Assert.True(Math.Abs(paar - (diffB - diffA)) <= 0.01,
                        "B gegen A (" + paar.ToString("N2", DE) + ") weicht von " +
                        "Δ(B) − Δ(A) (" + (diffB - diffA).ToString("N2", DE) + ") ab.");
        }

        /// <summary>
        /// ABNAHME § 2.15: Ein Tausch von A und B dreht das Vorzeichen von
        /// Kapitalwertdifferenz und Annuität.
        /// </summary>
        [Fact]
        public void Ein_Tausch_von_A_und_B_dreht_das_Vorzeichen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            double bGegenA = Erwartet(Rechne(VARIANTE_A), VARIANTE_B).KapitalwertDiff.Value;
            WirtschaftlichkeitErgebnis aGegenB = Erwartet(Rechne(VARIANTE_B), VARIANTE_A);

            Assert.Equal(-bGegenA, aGegenB.KapitalwertDiff.Value, 6);

            double annuitaetB = Erwartet(Rechne(VARIANTE_A), VARIANTE_B).AnnuitaetKW.Value;
            Assert.Equal(-annuitaetB, aGegenB.AnnuitaetKW.Value, 6);
        }

        /// <summary>
        /// RANDFALL (§ 2.9): Eine Referenz, die nicht in der Gruppe steht, wird zum
        /// Stamm — und die Gruppe trägt die Warnzeile, statt dass der Rückfall stumm
        /// bleibt.
        /// </summary>
        [Fact]
        public void Eine_fehlende_Referenz_traegt_die_Warnzeile_in_die_Gruppe()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            BerichtsDaten daten = Gruppendaten();
            new WirtschaftlichkeitCtrl().Berechne(daten, Parametersatz(0), 999999);

            Assert.Contains(daten.Warnungen, w => w.Contains("Stammprojekt", StringComparison.Ordinal));
        }

        // =================================================================
        // 5 — Die Zeilendefinition
        // =================================================================

        /// <summary>
        /// Ohne gewählte Referenz trägt der Stamm den Platzhalter „(Referenz)" — mit
        /// gewählter Referenz trägt ihn diese, und der Stamm bekommt seine Zahl.
        /// </summary>
        [Fact]
        public void Die_Zeilendefinition_kennzeichnet_die_gewaehlte_Referenz()
        {
            var stamm = new WirtschaftlichkeitErgebnis
            {
                IdProjekt = STAMM, IstStamm = true, Kapitalwert = -100.0
            };
            var variante = new WirtschaftlichkeitErgebnis
            {
                IdProjekt = VARIANTE_A, Kapitalwert = -60.0, KapitalwertDiff = 40.0
            };
            var menge = new List<WirtschaftlichkeitErgebnis> { stamm, variante };

            WirtZeile ohne = Zeile(WirtschaftlichkeitZeilen.Kennzahlen(menge, null));
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.WIRT_ZEILE_STAMM_REFERENZ, ohne.Anzeige(stamm, DE));
            Assert.Null(ohne.ExcelWert(stamm));

            stamm.KapitalwertDiff = -40.0;
            variante.KapitalwertDiff = null;
            WirtZeile mit = Zeile(WirtschaftlichkeitZeilen.Kennzahlen(menge, null, VARIANTE_A));
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.WIRT_ZEILE_STAMM_REFERENZ, mit.Anzeige(variante, DE));
            Assert.Null(mit.ExcelWert(variante));
            Assert.Equal(-40.0, mit.ExcelWert(stamm));
        }

        // =====================================================================
        // Hilfsmittel
        // =====================================================================

        private static WirtZeile Zeile(List<WirtZeile> zeilen)
        {
            WirtZeile z = zeilen.Find(x => x.Schluessel == "KAPITALWERT_DIFF");
            Assert.NotNull(z);
            return z;
        }

        private static WirtschaftlichkeitErgebnis Erwartet(
            List<WirtschaftlichkeitErgebnis> alle, int idProjekt)
        {
            WirtschaftlichkeitErgebnis e = alle.FirstOrDefault(
                x => x.Szenario == WirtschaftlichkeitSzenario.ERWARTET && x.IdProjekt == idProjekt);
            Assert.NotNull(e);
            return e;
        }

        /// <summary>Die drei Stände der Prüfgruppe als reine Id-Liste.</summary>
        private static List<VariantenDaten> Gruppe()
        {
            return new List<VariantenDaten>
            {
                new VariantenDaten { IdProjekt = STAMM, IstStamm = true, Projektname = "Stammprojekt" },
                new VariantenDaten { IdProjekt = VARIANTE_A, Variantenname = "Variante A",
                                     Projektname = "Stammprojekt" },
                new VariantenDaten { IdProjekt = VARIANTE_B, Variantenname = "Variante B",
                                     Projektname = "Stammprojekt" }
            };
        }

        /// <summary>
        /// Die Prüfgruppe: ein Stamm und zwei Varianten mit VERSCHIEDENEN Energiekosten
        /// — mehr braucht die Differenzrechnung nicht. Gemessen wird, WOGEGEN sie
        /// rechnet, nicht was in den Ständen steht.
        /// </summary>
        private static BerichtsDaten Gruppendaten()
        {
            var daten = new BerichtsDaten { IdStamm = STAMM, Stammprojektname = "Stammprojekt" };
            daten.Varianten.Add(Stand(STAMM, true, "Stammprojekt", 12000.0));
            daten.Varianten.Add(Stand(VARIANTE_A, false, "Variante A", 9000.0));
            daten.Varianten.Add(Stand(VARIANTE_B, false, "Variante B", 7000.0));
            return daten;
        }

        private static VariantenDaten Stand(int id, bool istStamm, string name, double energie)
        {
            return new VariantenDaten
            {
                IdProjekt = id,
                IstStamm = istStamm,
                Projektname = "Stammprojekt",
                Variantenname = istStamm ? "" : name,
                Ergebnis = new ErgebnisModel(),
                Energiekosten = energie
            };
        }

        private static WirtschaftlichkeitParameter Parametersatz(int idReferenz)
        {
            return new WirtschaftlichkeitParameter
            {
                IdStamm = STAMM,
                IdReferenzprojekt = idReferenz,
                Zinssatz = 3.0,
                Betrachtungszeitraum = 20,
                PreissteigerungEnergie = 0.0,
                PreissteigerungBetrieb = 0.0
            };
        }

        /// <summary>Rechnet die Prüfgruppe mit der gewünschten GRUPPENREFERENZ.</summary>
        private static List<WirtschaftlichkeitErgebnis> Rechne(int idReferenz)
        {
            return new WirtschaftlichkeitCtrl().Berechne(Gruppendaten(), Parametersatz(idReferenz));
        }
    }
}
