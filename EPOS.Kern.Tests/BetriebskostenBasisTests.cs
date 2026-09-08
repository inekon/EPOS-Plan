using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ANWENDERENTSCHEID W5‑B‑8 (09.09.2026): Die Betriebskosten-Bemessung
    /// „x % der Investitionssumme" (Kategorie 2) rechnet auf die KASKADE.
    ///
    /// <para><b>Der vierte Leseweg.</b> W5‑B‑7 hatte die H4b-Kaskade
    /// (<see cref="InvestKaskade"/>) zur einen Wahrheit der Kategorie 1 gemacht —
    /// Kapitalwertrechnung, Dialog und Kostenseite lesen sie seither gemeinsam.
    /// <c>BetriebskostenCtrl.InvestSummeFuer</c> (H4a) blieb aber bei der ROHEN
    /// Spaltensumme <c>SUM(EingegebenerWert)</c>: Die Betriebszeile „x % der
    /// Investitionssumme" bemaß sich damit an einer Zahl, die satzbasierte Zeilen
    /// (Menge × Satz) und alle Prozentzeilen der Investseite gar nicht enthielt.
    /// Der Entscheid vom 09.09.2026 schließt diesen vierten Weg — die Bezugsgröße ist
    /// jetzt die Kaskadensumme, in derselben Stufung Anlage → Komponente → Projekt und
    /// weiterhin VOR Zuschussabzug (K5).</para>
    ///
    /// <para><b>Der Träger der Fälle</b> ist das BHKW des Projekts 1018 (Komponente 7,
    /// Anlage 11327): Es führt als einzige Anlage der Testdatenbank sowohl die
    /// Kategorie-1-Kaskade als auch Kategorie-2-Zeilen „% der Investition". Die
    /// Gerätewelt liefert dafür eine feste Zahl — <c>Tab_BHKW.Pel</c> = 14,5 kW el —,
    /// damit auch die satzbasierte Zeile ohne geratene Größen rechnet.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class BetriebskostenBasisTests
    {
        private readonly ITestOutputHelper _ausgabe;

        public BetriebskostenBasisTests(ITestOutputHelper ausgabe) { _ausgabe = ausgabe; }

        private const int PROJEKT = 1018;
        private const int BHKW = 7;             // Tab_KostenKomponente.ID
        private const int ANLAGE = 11327;       // Tab_Energieanlagen.ID des BHKW in 1018

        private const double PEL = 14.5;        // Tab_BHKW.Pel der Anlage 11327 [kW el]

        // ---- Kategorie 1 (Investition) -------------------------------------------
        private const int I_HAUPT = 101600101;  // BETRAG 45.312,50 €, IsMainComponent
        private const int I_SATZ = 101600546;   // EUR_PRO_KW_ELEKTRISCH
        private const int I_ERZ = 101600549;    // PROZENT_ERZEUGERKOSTEN
        private const int I_P10 = 101600551;    // PROZENT_INVESTITION
        private const int I_P5 = 101600553;     // PROZENT_INVESTITION
        private static readonly int[] I_NULLZEILEN =
            { 101600547, 101600548, 101600550, 101600552 };

        // ---- Kategorie 2 (Betrieb) -----------------------------------------------
        private const int B_P2 = 101600555;     // PROZENT_INVESTITION, „Instandhaltung BHKW"

        // Die Zahlen des Beispiels — Schritt für Schritt nachrechenbar:
        //   direkte Zeile                            45.312,50 €
        //   200,00 €/kW el × 14,5 kW el               2.900,00 €   (satzbasiert)
        //   5 % der Erzeugerkosten (Runde 2)          2.265,625 €  (5 % von 45.312,50)
        //   ----------------------------------------------------
        //   Basis der Runde 3                        50.478,125 €
        //   10 % der Investition                      5.047,8125 €
        //    5 % der Investition                      2.523,90625 €
        //   ====================================================
        //   Kaskadensumme der Anlage                 58.049,84375 €
        private const double SATZ_JE_KW = 200.0;
        private const double BASIS_RUNDE3 = 50478.125;
        private const double KASKADE_ANLAGE = 58049.84375;
        private const double ROHSUMME_ANLAGE = 45312.5;   // die alte Basis (SUM(EingegebenerWert))
        private const double SATZ_BETRIEB = 2.0;          // % der Investitionssumme

        /// <summary>Legt das Beispiel in der ARBEITSKOPIE an.</summary>
        private static void BeispielAnlegen()
        {
            Setze(I_HAUPT, 45312.5, DbWerte.BEMESSUNG_BETRAG, null, DbWerte.KOSTENART_KAPITALGEBUNDEN);
            foreach (int id in I_NULLZEILEN)
                Setze(id, 0.0, DbWerte.BEMESSUNG_BETRAG, null, DbWerte.KOSTENART_KAPITALGEBUNDEN);
            Setze(I_SATZ, 0.0, DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH, SATZ_JE_KW,
                  DbWerte.KOSTENART_KAPITALGEBUNDEN);
            Setze(I_ERZ, 0.0, DbWerte.BEMESSUNG_PROZENT_ERZEUGERKOSTEN, 5.0,
                  DbWerte.KOSTENART_KAPITALGEBUNDEN);
            Setze(I_P10, 0.0, DbWerte.BEMESSUNG_PROZENT_INVESTITION, 10.0,
                  DbWerte.KOSTENART_KAPITALGEBUNDEN);
            Setze(I_P5, 0.0, DbWerte.BEMESSUNG_PROZENT_INVESTITION, 5.0,
                  DbWerte.KOSTENART_KAPITALGEBUNDEN);

            Setze(B_P2, 0.0, DbWerte.BEMESSUNG_PROZENT_INVESTITION, SATZ_BETRIEB,
                  DbWerte.KOSTENART_BETRIEBSGEBUNDEN);
        }

        private static void Setze(int id, double wert, string bemessung, double? satz, string kostenart)
        {
            var p = new DbParam("@e", DbParamTyp.Double);
            p.Wert = satz.HasValue ? (object)satz.Value : DBNull.Value;
            DataRepository.ExecuteSQL(
                "UPDATE Tab_ProjektWerte SET EingegebenerWert = ?, Bemessung = ?, " +
                "Einheitpreis = ?, Menge = NULL, Kostenart = ? WHERE ID = " + id,
                new DbParam("@w", wert),
                new DbParam("@b", bemessung),
                p,
                new DbParam("@k", kostenart));
        }

        // =====================================================================
        // Die neue Basis
        // =====================================================================

        /// <summary>
        /// Die Bezugsgröße der Betriebszeile ist die KASKADENSUMME der Anlage — also
        /// einschließlich der satzbasierten Zeile (Menge × Satz) und der beiden
        /// Prozentzeilen der Investseite, die die rohe Spaltensumme nie enthielt.
        /// </summary>
        [Fact]
        public void Basis_der_Betriebszeile_ist_die_Kaskadensumme_der_Anlage()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();

            // Die Investseite rechnet wie in InvestKaskadeTests belegt …
            Dictionary<int, InvestKaskade.Zeile> k =
                InvestKaskade.NachId(PROJEKT, WirtschaftlichkeitSzenario.ERWARTET);
            Assert.Equal(PEL * SATZ_JE_KW, k[I_SATZ].Betrag, 6);          // 2.900,00 €
            Assert.Equal(BASIS_RUNDE3 * 0.10, k[I_P10].Betrag, 6);        // 5.047,8125 €
            Assert.Equal(BASIS_RUNDE3, k[I_P10].Basis.Value, 6);

            // … und die Betriebsseite bemisst sich an deren SUMME.
            Assert.Equal(KASKADE_ANLAGE,
                BetriebskostenCtrl.InvestSummeFuer(PROJEKT, BHKW, ANLAGE).Value, 6);

            // Das Fehlerbild vor W5‑B‑8: die rohe Spaltensumme, also nur die direkte Zeile.
            Assert.NotEqual(ROHSUMME_ANLAGE,
                BetriebskostenCtrl.InvestSummeFuer(PROJEKT, BHKW, ANLAGE).Value);
        }

        /// <summary>
        /// Der Betrag der Betriebszeile und ihr Ausweis in der Nachweisliste rechnen mit
        /// dieser Basis — vorher 906,25 €/a (2 % von 45.312,50 €), nachher 1.160,996875 €/a
        /// (2 % von 58.049,84375 €).
        /// </summary>
        [Fact]
        public void Betriebszeile_und_Jahressumme_rechnen_mit_der_Kaskadenbasis()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();

            KostenPositionNachweis zeile = null;
            foreach (KostenPositionNachweis n in
                     WirtschaftlichkeitCtrl.LiesBetriebskostenPositionen(
                         PROJEKT, WirtschaftlichkeitSzenario.ERWARTET))
                if (n.Id == B_P2) zeile = n;

            Assert.NotNull(zeile);
            Assert.Equal(KASKADE_ANLAGE, zeile.Menge.Value, 6);                     // die Basis
            Assert.Equal(KASKADE_ANLAGE * SATZ_BETRIEB / 100.0, zeile.BetragJahr, 6);
            Assert.Equal(1160.996875, zeile.BetragJahr, 6);

            // Dieselbe Zahl in der Summenschleife (Kapitalwertrechnung, Kachel „Betrieb").
            // Alle übrigen Kategorie-2-Zeilen des Projekts 1018 tragen 0 (kein Satz).
            Assert.Equal(1160.996875,
                WirtschaftlichkeitCtrl.LiesBetriebskosten(
                    PROJEKT, WirtschaftlichkeitSzenario.ERWARTET), 6);

            // … und in der Kostenseite (Kategorie 2 je Anlagenzeile).
            Assert.Equal(1160.996875,
                KostenSummenCtrl.AnlagenSumme(PROJEKT, DbWerte.KOSTEN_KATEGORIE_BETRIEB, ANLAGE), 6);
        }

        /// <summary>
        /// PAKET FX5‑a: Der investgekoppelte AUSWEIS (Sensitivität „Investition ±10 %")
        /// zieht dieselbe Zeile mit — er muss mit der neuen Basis mitgewachsen sein,
        /// sonst skalierte der Ausschlag eine andere Investition als die, die I₀ zeigt.
        /// </summary>
        [Fact]
        public void Investgekoppelter_Ausweis_folgt_der_neuen_Basis()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();

            WirtschaftlichkeitCtrl.BetriebsTopfe t =
                WirtschaftlichkeitCtrl.LiesBetriebskostenTopfe(
                    PROJEKT, WirtschaftlichkeitSzenario.ERWARTET);

            Assert.Equal(1160.996875, t.InvestGekoppeltSofort, 6);
            Assert.Equal(t.Gesamt, t.InvestGekoppeltSofort, 6);   // hier ist es die ganze Summe
        }

        /// <summary>
        /// K5 bleibt: Eine Zuschusszeile hebt die Bezugsgröße nicht. Sie steht als
        /// POSITIVER Betrag in der Spalte — ohne den Ausschluss würde sie die
        /// Instandhaltung ERHÖHEN statt sie unberührt zu lassen.
        /// </summary>
        [Fact]
        public void Zuschuss_bleibt_ausserhalb_der_Betriebskostenbasis()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();

            // Erst als gewöhnliche Investitionszeile: die Basis wächst um 1.000 € — und um
            // die 15 % der beiden Prozentzeilen, die auf dieselbe Basis rechnen.
            DataRepository.ExecuteSQL(
                "UPDATE Tab_ProjektWerte SET EingegebenerWert = 1000 WHERE ID = " + I_NULLZEILEN[0]);
            Assert.Equal((BASIS_RUNDE3 + 1000.0) * 1.15,
                BetriebskostenCtrl.InvestSummeFuer(PROJEKT, BHKW, ANLAGE).Value, 6);

            // Als Zuschuss trägt dieselbe Zeile nichts mehr bei — weder zur Basis der
            // Runde 3 noch zur Summe der Anlage.
            DataRepository.ExecuteSQL(
                "UPDATE Tab_ProjektWerte SET Kostenart = ? WHERE ID = " + I_NULLZEILEN[0],
                new DbParam("@k", DbWerte.KOSTENART_ZUSCHUSS));

            Assert.Equal(KASKADE_ANLAGE,
                BetriebskostenCtrl.InvestSummeFuer(PROJEKT, BHKW, ANLAGE).Value, 6);
        }

        /// <summary>
        /// Die Stufung Anlage → Komponente → Projekt (H4a) gilt unverändert, jetzt auf den
        /// Kaskadensummen: Eine Betriebszeile AN der Anlage bemisst sich an deren
        /// Investition, eine Zeile ohne Anlagenbezug an der ganzen Komponente.
        /// </summary>
        [Fact]
        public void Basis_stuft_von_der_Anlage_auf_die_Komponente_zurueck()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();

            // Eine zweite direkte Investitionszeile der KOMPONENTE, aber ohne Anlagenbezug.
            DataRepository.ExecuteSQL(
                "UPDATE Tab_ProjektWerte SET EingegebenerWert = 4000, ID_Anlage = NULL " +
                "WHERE ID = " + I_NULLZEILEN[1]);

            // Solange die Betriebszeile an der Anlage hängt, bleibt es bei der Anlagensumme.
            Assert.Equal(KASKADE_ANLAGE,
                BetriebskostenCtrl.InvestSummeFuer(PROJEKT, BHKW, ANLAGE).Value, 6);

            // Ohne Anlagenbezug gilt die Komponentensumme — die 4.000 € kommen dazu.
            DataRepository.ExecuteSQL(
                "UPDATE Tab_ProjektWerte SET ID_Anlage = NULL WHERE ID = " + B_P2);
            Assert.Equal(KASKADE_ANLAGE + 4000.0,
                BetriebskostenCtrl.InvestSummeFuer(PROJEKT, BHKW, 0).Value, 6);

            KostenPositionNachweis zeile = null;
            foreach (KostenPositionNachweis n in
                     WirtschaftlichkeitCtrl.LiesBetriebskostenPositionen(
                         PROJEKT, WirtschaftlichkeitSzenario.ERWARTET))
                if (n.Id == B_P2) zeile = n;
            Assert.NotNull(zeile);
            Assert.Equal(KASKADE_ANLAGE + 4000.0, zeile.Menge.Value, 6);
        }

        /// <summary>
        /// ETAPPE H2-1: Der AUSWEIS nach <c>Tab_ProjektWerte.Menge</c> schreibt dieselbe
        /// Basis, mit der gerechnet wurde — sonst stünde im Dialog eine andere Herleitung
        /// als im Rechenweg. (In Kategorie 1 bleibt „% der Investition" wie bisher ohne
        /// Einzelzeilen-Ausweis: die Basis ist dort Kaskadenmaterie der Runde 3.)
        /// </summary>
        [Fact]
        public void Mengenausweis_der_Betriebszeile_traegt_die_Kaskadensumme()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();

            double? menge;
            Assert.True(WirtschaftlichkeitCtrl.MengeAusweisen(B_P2, out menge));
            Assert.Equal(KASKADE_ANLAGE, menge.Value, 6);

            object o = DataRepository.ExecuteScalar(
                "SELECT Menge FROM Tab_ProjektWerte WHERE ID = " + B_P2);
            Assert.Equal(KASKADE_ANLAGE, Convert.ToDouble(o), 6);

            // Kategorie 1 bleibt ausdrücklich ohne Ausweis.
            double? ohne;
            Assert.False(WirtschaftlichkeitCtrl.MengeAusweisen(I_P10, out ohne));
        }

        // =====================================================================
        // Regressionsprobe: was sich an den Bestandsprojekten ändert
        // =====================================================================

        /// <summary>
        /// DIE REGRESSIONSLISTE, die der Anwender sehen wollte (W5‑B‑8, Punkt 3): für jedes
        /// Projekt der Testdatenbank die Betriebskosten p. a. VORHER und NACHHER.
        ///
        /// <para><b>Wie „vorher" entsteht.</b> Nur die Bemessungsart „% der Investition"
        /// der Kategorie 2 ändert ihre Bezugsgröße; alles andere ist unberührt. Der Fall
        /// bildet deshalb die ALTE Regel — die rohe Spaltensumme, stufig
        /// Anlage → Komponente → Projekt, ohne Zuschusszeilen — noch einmal nach, rechnet
        /// jede solche Zeile mit beiden Basen über denselben
        /// <see cref="BetriebskostenCtrl.Betrag"/> und zieht die Differenz vom heutigen
        /// Ergebnis ab. Das ist exakt, weil der Betrag linear in der Basis ist.</para>
        ///
        /// <para><b>Ergebnis auf der unberührten <c>Kenndaten_Test.sqlite</c>: keine
        /// Abweichung.</b> Keine einzige Kostenzeile der Testdatenbank trägt einen Satz
        /// (<c>Einheitpreis</c> ist in allen 175 Kategorie-1/2-Zeilen NULL), und ohne Satz
        /// ist die Ableitung nach Anwenderentscheid I‑2 gar nicht rechenbar — es gilt der
        /// erfasste Wert, unabhängig von der Basis. Die Umstellung wird erst sichtbar,
        /// sobald ein Satz gepflegt ist; genau das zeigen die Fälle oben.</para>
        /// </summary>
        [Fact]
        public void Betriebskosten_je_Projekt_vorher_und_nachher()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            _ausgabe.WriteLine("Projekt | Betriebskosten p. a. vorher | nachher | Abweichung");
            _ausgabe.WriteLine("--------|-----------------------------|---------|-----------");

            foreach (int projekt in Projekte())
            {
                double nachher = WirtschaftlichkeitCtrl.LiesBetriebskosten(
                    projekt, WirtschaftlichkeitSzenario.ERWARTET);
                double vorher = nachher - BasisWechselDelta(projekt);

                _ausgabe.WriteLine(string.Format(CultureInfo.GetCultureInfo("de-DE"),
                    "{0,7} | {1,27:N2} | {2,7:N2} | {3,9:N2}",
                    projekt, vorher, nachher, nachher - vorher));

                Assert.Equal(vorher, nachher, 6);
            }
        }

        private static List<int> Projekte()
        {
            var liste = new List<int>();
            DataTable dt = DataRepository.GetDataTable(
                "SELECT DISTINCT ProjektID FROM Tab_ProjektWerte ORDER BY ProjektID");
            foreach (DataRow r in dt.Rows)
                if (r["ProjektID"] != DBNull.Value) liste.Add(Convert.ToInt32(r["ProjektID"]));
            return liste;
        }

        /// <summary>Summe der Betragsänderungen, die allein aus dem Basiswechsel folgen.</summary>
        private static double BasisWechselDelta(int projekt)
        {
            double delta = 0;
            DataTable dt = DataRepository.GetDataTable(
                "SELECT KomponentenID, [" + SchemaKatalog.SPALTE_PW_ID_ANLAGE + "], " +
                "EingegebenerWert, [" + SchemaKatalog.SPALTE_PW_EINHEITPREIS + "], " +
                "[" + SchemaKatalog.SPALTE_PW_IST_ERLOES + "] " +
                "FROM Tab_ProjektWerte WHERE ProjektID = ? AND KategorieID = ? AND [" +
                SchemaKatalog.SPALTE_PW_BEMESSUNG + "] = ?",
                new DbParam("@p", projekt),
                new DbParam("@k", DbWerte.KOSTEN_KATEGORIE_BETRIEB),
                new DbParam("@b", DbWerte.BEMESSUNG_PROZENT_INVESTITION));
            if (dt == null) return 0;

            foreach (DataRow r in dt.Rows)
            {
                int komponente = r["KomponentenID"] == DBNull.Value
                    ? 0 : Convert.ToInt32(r["KomponentenID"]);
                int anlage = r[SchemaKatalog.SPALTE_PW_ID_ANLAGE] == DBNull.Value
                    ? 0 : Convert.ToInt32(r[SchemaKatalog.SPALTE_PW_ID_ANLAGE]);
                double eingegeben = r["EingegebenerWert"] == DBNull.Value
                    ? 0 : Convert.ToDouble(r["EingegebenerWert"]);
                double? satz = r[SchemaKatalog.SPALTE_PW_EINHEITPREIS] == DBNull.Value
                    ? (double?)null : Convert.ToDouble(r[SchemaKatalog.SPALTE_PW_EINHEITPREIS]);
                bool erloes = r[SchemaKatalog.SPALTE_PW_IST_ERLOES] != DBNull.Value &&
                              Convert.ToBoolean(r[SchemaKatalog.SPALTE_PW_IST_ERLOES]);

                double neu = BetriebskostenCtrl.Betrag(
                    DbWerte.BEMESSUNG_PROZENT_INVESTITION, eingegeben,
                    BetriebskostenCtrl.InvestSummeFuer(projekt, komponente, anlage), satz, erloes);
                double alt = BetriebskostenCtrl.Betrag(
                    DbWerte.BEMESSUNG_PROZENT_INVESTITION, eingegeben,
                    AlteBasis(projekt, komponente, anlage), satz, erloes);
                delta += neu - alt;
            }
            return delta;
        }

        /// <summary>Die Regel VOR W5‑B‑8, hier allein zum Vergleich nachgebildet:
        /// <c>SUM(EingegebenerWert)</c> der Kategorie-1-Zeilen ohne Zuschuss, stufig
        /// Anlage → Komponente → Projekt.</summary>
        private static double? AlteBasis(int projekt, int komponente, int anlage)
        {
            if (anlage > 0)
            {
                double? a = Rohsumme(projekt, komponente, anlage);
                if (a.HasValue && a.Value != 0) return a;
            }
            if (komponente > 0)
            {
                double? k = Rohsumme(projekt, komponente, 0);
                if (k.HasValue && k.Value != 0) return k;
            }
            return Rohsumme(projekt, 0, 0);
        }

        private static double? Rohsumme(int projekt, int komponente, int anlage)
        {
            var ps = new List<DbParam>
            {
                new DbParam("@p", projekt),
                new DbParam("@k", DbWerte.KOSTEN_KATEGORIE_INVESTITION),
                new DbParam("@a", DbWerte.KOSTENART_ZUSCHUSS)
            };
            string sql = "SELECT SUM(EingegebenerWert) FROM Tab_ProjektWerte " +
                         "WHERE ProjektID = ? AND KategorieID = ? AND " +
                         "(([" + SchemaKatalog.SPALTE_PW_KOSTENART + "] IS NULL) OR ([" +
                         SchemaKatalog.SPALTE_PW_KOSTENART + "] <> ?))";
            if (komponente > 0)
            {
                sql += " AND KomponentenID = ?";
                ps.Add(new DbParam("@c", komponente));
            }
            if (anlage > 0)
            {
                sql += " AND [" + SchemaKatalog.SPALTE_PW_ID_ANLAGE + "] = ?";
                ps.Add(new DbParam("@n", anlage));
            }
            object o = DataRepository.ExecuteScalar(sql, ps.ToArray());
            return (o == null || o == DBNull.Value) ? (double?)null : Convert.ToDouble(o);
        }
    }
}
