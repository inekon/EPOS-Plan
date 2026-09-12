using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ANWENDERENTSCHEID W5‑B‑9 (09.09.2026): „Szenarioparameter umsetzen."
    ///
    /// <para><b>Der Befund vom 08.09.2026:</b> Die Seite „Wirtschaftlichkeit" bot die drei
    /// Szenarien Erwartet / Best / Worst an und zeigte in allen dreien dieselben Zahlen.
    /// Sie unterschieden sich ausschließlich über die ZEILENwerte
    /// <c>Tab_ProjektWerte.BestCase</c>/<c>WorstCase</c> — und die stehen im Bestand bei
    /// nahezu jeder Position auf 0, worauf <see cref="WirtschaftlichkeitCtrl.Szenariowert"/>
    /// nach dem VALERI-Muster auf den Erwartungswert zurückfällt.</para>
    ///
    /// <para><b>Was diese Fälle festhalten</b> (Konzept_Wirtschaftlichkeit_Szenarien_VALERI.md):
    /// die Vorgaben und ihre Vorzeichen, die Nullsemantik („leer = Vorgabe, nicht 0"), die
    /// Vorrangregel gepflegter Zeilenwerte, die Zusage <b>Erwartet bleibt zahlengleich</b>
    /// und die Reihenfolge KW_Best ≥ KW_Erwartet ≥ KW_Worst.</para>
    ///
    /// <para><b>Der Träger der Datenbankfälle</b> ist die Wärmepumpe des Projekts 1040 —
    /// dieselben Zeilen, mit denen <see cref="InvestKaskadeTests"/> die Kaskade belegt.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class SzenarioParameterTests
    {
        private const int PROJEKT = 1040;

        private const int ID_HAUPT = 101600487;   // BETRAG, IsMainComponent
        private const int ID_P3 = 101600494;      // PROZENT_INVESTITION 3 %
        private const int ID_P10A = 101600495;
        private const int ID_P10B = 101600497;
        private static readonly int[] ID_NULLZEILEN =
            { 101600491, 101600492, 101600493, 101600496 };

        private const double HAUPT = 5660.0;

        private static readonly CultureInfo DE = new CultureInfo("de-DE");

        // =====================================================================
        // SzenarioSatz — Vorgaben, Vorzeichen, Nullsemantik
        // =====================================================================

        /// <summary>
        /// Die Vorgaben stehen in beide Richtungen: Best günstiger, Worst ungünstiger.
        /// Zins und Preissteigerungen gelten ABSOLUT (Projektwert ∓ 1 %-Punkt),
        /// Investition und Erträge als Änderung in Prozent, die Nutzungsdauer in Jahren.
        /// </summary>
        [Fact]
        public void Die_Vorgaben_zeigen_in_beide_Richtungen()
        {
            var p = new WirtschaftlichkeitParameter
            { Zinssatz = 3.0, PreissteigerungEnergie = 2.0, PreissteigerungBetrieb = 1.0 };

            SzenarioSatz best = p.SatzFuer(WirtschaftlichkeitSzenario.BEST);
            SzenarioSatz worst = p.SatzFuer(WirtschaftlichkeitSzenario.WORST);

            Assert.Equal(2.0, best.ZinsWirksam(p.Zinssatz), 9);
            Assert.Equal(4.0, worst.ZinsWirksam(p.Zinssatz), 9);
            Assert.Equal(1.0, best.PreisEnergieWirksam(p.PreissteigerungEnergie), 9);
            Assert.Equal(3.0, worst.PreisEnergieWirksam(p.PreissteigerungEnergie), 9);
            Assert.Equal(0.0, best.PreisBetriebWirksam(p.PreissteigerungBetrieb), 9);
            Assert.Equal(2.0, worst.PreisBetriebWirksam(p.PreissteigerungBetrieb), 9);

            // + heisst mehr bzw. laenger: billiger investieren und laenger nutzen ist gut.
            Assert.Equal(-10.0, best.InvestWirksam, 9);
            Assert.Equal(+10.0, worst.InvestWirksam, 9);
            Assert.Equal(+10.0, best.ErtragWirksam, 9);
            Assert.Equal(-10.0, worst.ErtragWirksam, 9);
            Assert.Equal(+2.0, best.DauerWirksam, 9);
            Assert.Equal(-2.0, worst.DauerWirksam, 9);

            Assert.Equal(0.9, best.InvestFaktor, 9);
            Assert.Equal(1.1, best.ErtragFaktor, 9);
        }

        /// <summary>
        /// Ein NICHT gepflegtes Feld zieht bei einer geänderten Projektangabe mit — genau
        /// dafür ist <c>null</c> da und nicht 0. Ein gepflegtes Feld bleibt stehen.
        /// </summary>
        [Fact]
        public void Ein_leeres_Feld_zieht_mit_ein_gepflegtes_nicht()
        {
            var p = new WirtschaftlichkeitParameter { Zinssatz = 3.0 };
            SzenarioSatz best = p.SatzFuer(WirtschaftlichkeitSzenario.BEST);

            Assert.True(best.NurVorgaben);
            Assert.Equal(2.0, best.ZinsWirksam(p.Zinssatz), 9);

            p.Zinssatz = 6.0;                       // Projektangabe geaendert
            Assert.Equal(5.0, best.ZinsWirksam(p.Zinssatz), 9);

            best.Zinssatz = 1.25;                   // jetzt gepflegt
            Assert.False(best.NurVorgaben);
            p.Zinssatz = 9.0;
            Assert.Equal(1.25, best.ZinsWirksam(p.Zinssatz), 9);
        }

        /// <summary>Ein negativer Kalkulationszins wäre keine Investitionsrechnung mehr —
        /// die Vorgabe wird bei 0 geklemmt.</summary>
        [Fact]
        public void Der_Zins_wird_nie_negativ()
        {
            var p = new WirtschaftlichkeitParameter { Zinssatz = 0.5 };
            Assert.Equal(0.0, p.SatzFuer(WirtschaftlichkeitSzenario.BEST).ZinsWirksam(p.Zinssatz), 9);
        }

        /// <summary>
        /// Die Nutzungsdauer wird ADDIERT und bei 1 a geklemmt. Zeilen ohne (sinnvoll)
        /// gepflegte Nutzungsdauer (n &lt; 1) bleiben unberührt — dort heißt der Wert im
        /// <see cref="KapitalwertRechner"/> „wie der Betrachtungszeitraum", und das ist
        /// keine Dauer, die man verlängern könnte.
        /// </summary>
        [Fact]
        public void Die_Nutzungsdauer_wird_addiert_und_bei_einem_Jahr_geklemmt()
        {
            SzenarioSatz worst = SzenarioSatz.Vorgabe(WirtschaftlichkeitSzenario.WORST);

            Assert.Equal(18.0, worst.DauerFuer(20.0), 9);
            Assert.Equal(1.0, worst.DauerFuer(1.5), 9);     // 1,5 - 2 = -0,5 -> 1
            Assert.Equal(0.0, worst.DauerFuer(0.0), 9);     // "keine Dauer" bleibt "keine Dauer"
        }

        /// <summary>
        /// ERWARTET bekommt keinen Satz — und <see cref="WirtschaftlichkeitParameter.FuerSzenario"/>
        /// gibt dieselbe REFERENZ zurück. Genau daran hängt die Zusage, dass der
        /// Erwartungsfall den Rechenweg von vor dieser Etappe geht.
        /// </summary>
        [Fact]
        public void Erwartet_bekommt_denselben_Parametersatz_zurueck()
        {
            var p = new WirtschaftlichkeitParameter { Zinssatz = 3.0, PreissteigerungEnergie = 2.0 };

            Assert.Null(p.SatzFuer(WirtschaftlichkeitSzenario.ERWARTET));
            Assert.Same(p, p.FuerSzenario(WirtschaftlichkeitSzenario.ERWARTET));
            Assert.Same(p, p.FuerSzenario("etwas anderes"));

            WirtschaftlichkeitParameter best = p.FuerSzenario(WirtschaftlichkeitSzenario.BEST);
            Assert.NotSame(p, best);
            Assert.Equal(2.0, best.Zinssatz, 9);
            Assert.Equal(1.0, best.PreissteigerungEnergie, 9);
            Assert.Equal(3.0, p.Zinssatz, 9);        // das Original bleibt unberuehrt
        }

        /// <summary>Die Nachweiszeile nennt die WIRKSAMEN Zahlen mit Vorzeichen.</summary>
        [Fact]
        public void Die_Nachweiszeile_nennt_die_wirksamen_Zahlen()
        {
            var p = new WirtschaftlichkeitParameter
            { Zinssatz = 3.0, PreissteigerungEnergie = 2.0, PreissteigerungBetrieb = 1.0 };

            string zeile = p.SatzFuer(WirtschaftlichkeitSzenario.WORST).Nachweis(p, DE);

            Assert.Contains("i = 4,0 %", zeile);
            Assert.Contains("p_E = 3,0 %/a", zeile);
            Assert.Contains("Investition +10 %", zeile);
            Assert.Contains("Erträge -10 %", zeile);
            Assert.Contains("Nutzungsdauer -2 a", zeile);
        }

        // =====================================================================
        // Szenariowert — die Herkunft
        // =====================================================================

        /// <summary>
        /// <see cref="WirtschaftlichkeitCtrl.Szenariowert"/> sagt seit W5‑B‑9, ob der Wert
        /// GEPFLEGT war oder auf den Erwartungswert zurückgefallen ist. Der Rückgabewert
        /// selbst ist unverändert.
        /// </summary>
        [Fact]
        public void Szenariowert_meldet_die_Herkunft()
        {
            var t = new DataTable();
            t.Columns.Add("EingegebenerWert", typeof(double));
            t.Columns.Add("BestCase", typeof(double));
            t.Columns.Add("WorstCase", typeof(double));
            DataRow r = t.Rows.Add(100.0, 0.0, 130.0);

            bool gepflegt;

            Assert.Equal(100.0, WirtschaftlichkeitCtrl.Szenariowert(
                r, WirtschaftlichkeitSzenario.ERWARTET,
                "EingegebenerWert", "BestCase", "WorstCase", out gepflegt), 9);
            Assert.False(gepflegt);                  // ERWARTET fragt keine Szenariospalte

            Assert.Equal(100.0, WirtschaftlichkeitCtrl.Szenariowert(
                r, WirtschaftlichkeitSzenario.BEST,
                "EingegebenerWert", "BestCase", "WorstCase", out gepflegt), 9);
            Assert.False(gepflegt);                  // 0/leer = nicht gepflegt

            Assert.Equal(130.0, WirtschaftlichkeitCtrl.Szenariowert(
                r, WirtschaftlichkeitSzenario.WORST,
                "EingegebenerWert", "BestCase", "WorstCase", out gepflegt), 9);
            Assert.True(gepflegt);
        }

        // =====================================================================
        // Investitionspositionen — Vorrang und Ausschlag
        // =====================================================================

        /// <summary>
        /// OHNE Szenariosatz ist die Investitionsliste Zeichen für Zeichen die von vor
        /// dieser Etappe — das ist der Weg des Szenarios ERWARTET und jeder Anzeige, die
        /// erfasste Zahlen zeigt.
        /// </summary>
        [Fact]
        public void Ohne_Satz_bleibt_die_Investitionsliste_unveraendert()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();

            double zuschussA, zuschussB;
            List<KapitalwertRechner.InvestPosition> alt = WirtschaftlichkeitCtrl.LiesInvestitionen(
                PROJEKT, WirtschaftlichkeitSzenario.ERWARTET, out zuschussA);
            List<KapitalwertRechner.InvestPosition> neu = WirtschaftlichkeitCtrl.LiesInvestitionen(
                PROJEKT, WirtschaftlichkeitSzenario.ERWARTET, null, out zuschussB);

            Assert.NotEmpty(alt);
            Assert.Equal(alt.Count, neu.Count);
            for (int i = 0; i < alt.Count; i++)
            {
                // BITgleich, nicht nur gerundet gleich.
                Assert.Equal(alt[i].Betrag, neu[i].Betrag);
                Assert.Equal(alt[i].Nutzungsdauer, neu[i].Nutzungsdauer);
                Assert.Equal(alt[i].StartJahr, neu[i].StartJahr);
            }
            Assert.Equal(zuschussA, zuschussB);
        }

        /// <summary>
        /// Mit Satz skaliert der pauschale Ausschlag jede Zeile OHNE gepflegten
        /// Szenariowert — hier alle vier, also die ganze Kaskadensumme.
        /// </summary>
        [Fact]
        public void Der_Ausschlag_greift_auf_nicht_gepflegte_Zeilen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();

            double egal;
            SzenarioSatz worst = SzenarioSatz.Vorgabe(WirtschaftlichkeitSzenario.WORST);
            SzenarioSatz best = SzenarioSatz.Vorgabe(WirtschaftlichkeitSzenario.BEST);

            // BeispielAnlegen raeumt alle Szenariospalten des Projekts leer - es ist
            // also KEINE Zeile gepflegt, und der Ausschlag greift auf alle.
            double erwartet = Summe(WirtschaftlichkeitCtrl.LiesInvestitionen(
                PROJEKT, WirtschaftlichkeitSzenario.ERWARTET, null, out egal));
            Assert.True(erwartet > 0);

            Assert.Equal(erwartet * 1.1, Summe(WirtschaftlichkeitCtrl.LiesInvestitionen(
                PROJEKT, WirtschaftlichkeitSzenario.WORST, worst, out egal)), 6);
            Assert.Equal(erwartet * 0.9, Summe(WirtschaftlichkeitCtrl.LiesInvestitionen(
                PROJEKT, WirtschaftlichkeitSzenario.BEST, best, out egal)), 6);
        }

        /// <summary>
        /// DIE VORRANGREGEL: Ein je Zeile gepflegter Worst-Case-Betrag gilt genau so, wie
        /// er dasteht — nicht mal 1,1. Wer 7.000 € erfasst hat, meint diese Zahl.
        /// </summary>
        [Fact]
        public void Ein_gepflegter_Zeilenwert_schlaegt_den_pauschalen_Ausschlag()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();
            DataRepository.ExecuteSQL(
                "UPDATE Tab_ProjektWerte SET WorstCase = 7000 WHERE ID = " + ID_HAUPT);

            double egal;
            SzenarioSatz worst = SzenarioSatz.Vorgabe(WirtschaftlichkeitSzenario.WORST);
            List<KapitalwertRechner.InvestPosition> liste = WirtschaftlichkeitCtrl.LiesInvestitionen(
                PROJEKT, WirtschaftlichkeitSzenario.WORST, worst, out egal);

            // Die Kaskade weiss, woher der Wert kam - daran haengt die Vorrangregel.
            Dictionary<int, InvestKaskade.Zeile> k =
                InvestKaskade.NachId(PROJEKT, WirtschaftlichkeitSzenario.WORST);
            Assert.True(k[ID_HAUPT].WertGepflegt);
            Assert.False(k[ID_P3].WertGepflegt);
            Assert.False(k[ID_P10A].WertGepflegt);
            Assert.Equal(7000.0, k[ID_HAUPT].Betrag, 6);

            // Die Hauptzeile steht mit 7.000,00 - unskaliert, nicht mit 7.700,00.
            Assert.Contains(liste, x => Math.Abs(x.Betrag - 7000.0) < 1e-9);
            Assert.DoesNotContain(liste, x => Math.Abs(x.Betrag - 7700.0) < 1e-9);

            // Die drei Prozentzeilen haben KEINEN eigenen Worst-Wert. Ihre Basis ist die
            // gepflegten 7.000,00 (nicht 5.660,00), und sie werden zusaetzlich skaliert:
            // 3 % / 10 % / 10 % von 7.000 = 210 / 700 / 700, davon je 110 %.
            Assert.Equal(210.0 * 1.1, Betrag(liste, 210.0 * 1.1), 6);
            Assert.Equal(700.0 * 1.1, Betrag(liste, 700.0 * 1.1), 6);
        }

        /// <summary>
        /// Eine Zuschusszeile bleibt vom Ausschlag unberührt (K5): Eine bewilligte
        /// Förderzusage über einen festen Betrag ändert sich nicht, weil die Anlage 10 %
        /// mehr kostet.
        /// </summary>
        [Fact]
        public void Der_Zuschuss_wird_vom_Ausschlag_nicht_skaliert()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();
            DataRepository.ExecuteSQL(
                "UPDATE Tab_ProjektWerte SET EingegebenerWert = 1500, Bemessung = ?, " +
                "Einheitpreis = NULL, Menge = NULL, Kostenart = ? WHERE ID = " + ID_NULLZEILEN[0],
                new DbParam("@b", DbWerte.BEMESSUNG_BETRAG),
                new DbParam("@k", DbWerte.KOSTENART_ZUSCHUSS));

            double zuschuss;
            WirtschaftlichkeitCtrl.LiesInvestitionen(
                PROJEKT, WirtschaftlichkeitSzenario.WORST,
                SzenarioSatz.Vorgabe(WirtschaftlichkeitSzenario.WORST), out zuschuss);

            Assert.Equal(1500.0, zuschuss, 6);
        }

        /// <summary>
        /// Die Nutzungsdaueränderung greift ebenfalls nur auf Zeilen ohne gepflegte
        /// Szenario-Nutzungsdauer — und sie ändert über Ersatzbeschaffung und Restwert
        /// den Kapitalwert.
        /// </summary>
        [Fact]
        public void Die_Nutzungsdauer_folgt_dem_Satz_und_der_Vorrangregel()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();
            DataRepository.ExecuteSQL(
                "UPDATE Tab_ProjektWerte SET Nutzungsdauer = 20, WorstCase_Nutzungsdauer = 0 " +
                "WHERE ID = " + ID_HAUPT);
            DataRepository.ExecuteSQL(
                "UPDATE Tab_ProjektWerte SET Nutzungsdauer = 20, WorstCase_Nutzungsdauer = 12 " +
                "WHERE ID = " + ID_P3);

            double egal;
            List<KapitalwertRechner.InvestPosition> liste = WirtschaftlichkeitCtrl.LiesInvestitionen(
                PROJEKT, WirtschaftlichkeitSzenario.WORST,
                SzenarioSatz.Vorgabe(WirtschaftlichkeitSzenario.WORST), out egal);

            // ohne gepflegte Szenario-Dauer: 20 - 2 = 18; mit gepflegter: 12, unveraendert.
            Assert.Contains(liste, x => Math.Abs(x.Betrag - HAUPT * 1.1) < 1e-6 &&
                                        Math.Abs(x.Nutzungsdauer - 18.0) < 1e-9);
            Assert.Contains(liste, x => Math.Abs(x.Nutzungsdauer - 12.0) < 1e-9);
        }

        // =====================================================================
        // Kapitalwert — die Reihenfolge und die Zahlengleichheit
        // =====================================================================

        /// <summary>
        /// Die Probe des Entscheids: Mit den Vorgaben liegen die drei Szenarien
        /// AUSEINANDER, und zwar in der richtigen Reihenfolge KW_Best ≥ KW_Erwartet ≥
        /// KW_Worst. Gerechnet wird mit dem echten Rechenkern über die echten
        /// Investitionspositionen des Projekts 1040; die übrigen Zahlungsgrößen sind
        /// bewusst feste Ansätze, damit der Fall nur die Szenariowirkung misst.
        /// </summary>
        [Fact]
        public void Best_Erwartet_und_Worst_liegen_auseinander()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();

            var p = new WirtschaftlichkeitParameter
            {
                Zinssatz = 3.0,
                Betrachtungszeitraum = 20,
                PreissteigerungEnergie = 2.0,
                PreissteigerungBetrieb = 1.0
            };

            double kwErwartet = Kapitalwert(p, WirtschaftlichkeitSzenario.ERWARTET);
            double kwBest = Kapitalwert(p, WirtschaftlichkeitSzenario.BEST);
            double kwWorst = Kapitalwert(p, WirtschaftlichkeitSzenario.WORST);

            Assert.True(kwBest > kwErwartet,
                        "Best " + kwBest + " liegt nicht ueber Erwartet " + kwErwartet);
            Assert.True(kwErwartet > kwWorst,
                        "Erwartet " + kwErwartet + " liegt nicht ueber Worst " + kwWorst);
        }

        /// <summary>
        /// DIE ZUSAGE DES ENTSCHEIDS: <b>Erwartet bleibt zahlengleich.</b> Der
        /// Erwartungsfall darf sich weder ändern, wenn Vorgaben gelten, noch wenn jemand
        /// Best und Worst von Hand pflegt — er bekommt gar keinen Satz.
        /// </summary>
        [Fact]
        public void Erwartet_bleibt_zahlengleich()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();

            var p = new WirtschaftlichkeitParameter
            {
                Zinssatz = 3.0,
                Betrachtungszeitraum = 20,
                PreissteigerungEnergie = 2.0,
                PreissteigerungBetrieb = 1.0
            };
            double vorher = Kapitalwert(p, WirtschaftlichkeitSzenario.ERWARTET);

            p.SatzBest.Zinssatz = 0.5;
            p.SatzBest.InvestitionAenderung = -40.0;
            p.SatzWorst.Zinssatz = 12.0;
            p.SatzWorst.InvestitionAenderung = 80.0;
            p.SatzWorst.NutzungsdauerAenderung = -10.0;

            Assert.Equal(vorher, Kapitalwert(p, WirtschaftlichkeitSzenario.ERWARTET), 12);
        }

        // =====================================================================
        // Ablage (Migrationsschritt 71)
        // =====================================================================

        /// <summary>Der Zielstand ist 71, und die zwölf Spalten stehen im Katalog.</summary>
        [Fact]
        public void Der_Migrationsschritt_71_fuehrt_zwoelf_Spalten()
        {
            Assert.True(SchemaStand.Zielversion >= 71,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter 71.");

            var namen = new List<string>();
            foreach (SchemaSpalte s in SchemaKatalog.Schritt71_Szenarioparameter)
            {
                Assert.Equal(SchemaKatalog.TAB_PROJEKTWIRTSCHAFT, s.Tabelle);
                Assert.Equal("DOUBLE", s.TypDefinition);
                namen.Add(s.Name);
            }
            Assert.Equal(12, namen.Count);
            Assert.Equal(12, new HashSet<string>(namen).Count);
            Assert.Contains(SchemaKatalog.SPALTE_PW_SZEN_BEST_ZINS, namen);
            Assert.Contains(SchemaKatalog.SPALTE_PW_SZEN_WORST_DAUER, namen);
        }

        /// <summary>
        /// Die Spalten sind in der Testdatenbank angelegt (Schritt 71), und der
        /// Speicherweg legt sie hin und holt sie zurück — <c>null</c> bleibt <c>null</c>,
        /// eine gepflegte Zahl bleibt die Zahl.
        /// </summary>
        [Fact]
        public void Der_Satz_ueberlebt_Speichern_und_Laden()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            foreach (SchemaSpalte s in SchemaKatalog.Schritt71_Szenarioparameter)
                Assert.True(DataRepository.SpalteVorhanden(s.Tabelle, s.Name),
                            "Spalte fehlt: " + s.Tabelle + "." + s.Name);

            var ctrl = new WirtschaftlichkeitCtrl();
            WirtschaftlichkeitParameter p = ctrl.LadeParameter(PROJEKT);
            Assert.True(p.SatzBest.NurVorgaben);      // frisch migriert: alles NULL

            p.SatzBest.Zinssatz = 1.75;
            p.SatzWorst.InvestitionAenderung = 25.0;
            p.SatzWorst.NutzungsdauerAenderung = -4.0;
            Assert.True(ctrl.SpeichereParameter(p));

            WirtschaftlichkeitParameter zurueck = new WirtschaftlichkeitCtrl().LadeParameter(PROJEKT);
            Assert.Equal(1.75, zurueck.SatzBest.Zinssatz);
            Assert.Null(zurueck.SatzBest.PreissteigerungEnergie);      // leer bleibt leer
            Assert.Equal(25.0, zurueck.SatzWorst.InvestitionAenderung);
            Assert.Equal(-4.0, zurueck.SatzWorst.NutzungsdauerAenderung);
            Assert.False(zurueck.SatzBest.NurVorgaben);
        }

        // =====================================================================
        // W5-B-10 (VALERI) — Ersatzbeschaffungen als eigener Ausweis
        // =====================================================================

        /// <summary>
        /// Die Zeile „Ersatzbeschaffungen, Barwert" erscheint nur, wo es Ersatz gibt —
        /// DIN EN 17463 verlangt die Position, eine Zeile aus lauter Nullen wäre keine.
        /// </summary>
        [Fact]
        public void Die_Ersatzzeile_erscheint_nur_mit_Ersatzbeschaffungen()
        {
            var ohne = new List<WirtschaftlichkeitErgebnis>
            { new WirtschaftlichkeitErgebnis { Kapitalwert = -1000, ErsatzBarwert = 0 } };
            var mit = new List<WirtschaftlichkeitErgebnis>
            { new WirtschaftlichkeitErgebnis { Kapitalwert = -1000, ErsatzBarwert = 4200.5 } };

            Assert.DoesNotContain(WirtschaftlichkeitZeilen.Kennzahlen(ohne, new TarifParameter()),
                                  z => z.Schluessel == "ERSATZ");

            List<WirtZeile> zeilen = new List<WirtZeile>(
                WirtschaftlichkeitZeilen.Kennzahlen(mit, new TarifParameter()));
            WirtZeile ersatz = zeilen.Find(z => z.Schluessel == "ERSATZ");
            Assert.NotNull(ersatz);
            Assert.Equal(4200.5, ersatz.Wert(mit[0]).Value, 6);

            // Sie steht VOR dem Restwert - beide gehoeren zusammen (VALERI).
            Assert.True(zeilen.FindIndex(z => z.Schluessel == "ERSATZ") <
                        zeilen.FindIndex(z => z.Schluessel == "RESTWERT"));
        }

        // =====================================================================
        // Hilfsmittel
        // =====================================================================

        /// <summary>Legt das Beispiel der <see cref="InvestKaskadeTests"/> in der
        /// ARBEITSKOPIE an: 5.660,00 € direkt, dazu 3 % / 10 % / 10 % der Investition.</summary>
        private static void BeispielAnlegen()
        {
            Setze(ID_HAUPT, HAUPT, DbWerte.BEMESSUNG_BETRAG, null);
            foreach (int id in ID_NULLZEILEN) Setze(id, 0.0, DbWerte.BEMESSUNG_BETRAG, null);
            Setze(ID_P3, 0.0, DbWerte.BEMESSUNG_PROZENT_INVESTITION, 3.0);
            Setze(ID_P10A, 0.0, DbWerte.BEMESSUNG_PROZENT_INVESTITION, 10.0);
            Setze(ID_P10B, 0.0, DbWerte.BEMESSUNG_PROZENT_INVESTITION, 10.0);
            DataRepository.ExecuteSQL(
                "UPDATE Tab_ProjektWerte SET BestCase = 0, WorstCase = 0, " +
                "BestCase_Nutzungsdauer = 0, WorstCase_Nutzungsdauer = 0 " +
                "WHERE ProjektID = " + PROJEKT + " AND KategorieID = 1");
        }

        private static void Setze(int id, double wert, string bemessung, double? satz)
        {
            var p = new DbParam("@e", DbParamTyp.Double);
            p.Wert = satz.HasValue ? (object)satz.Value : DBNull.Value;
            DataRepository.ExecuteSQL(
                "UPDATE Tab_ProjektWerte SET EingegebenerWert = ?, Bemessung = ?, " +
                "Einheitpreis = ?, Menge = NULL, Kostenart = ? WHERE ID = " + id,
                new DbParam("@w", wert),
                new DbParam("@b", bemessung),
                p,
                new DbParam("@k", DbWerte.KOSTENART_KAPITALGEBUNDEN));
        }

        /// <summary>Der Betrag der Position, die dem gesuchten Wert am naechsten liegt —
        /// so steht der IST-Wert bei einem Fehlschlag in der Meldung.</summary>
        private static double Betrag(List<KapitalwertRechner.InvestPosition> liste, double gesucht)
        {
            double treffer = double.NaN, abstand = double.MaxValue;
            foreach (KapitalwertRechner.InvestPosition pos in liste)
                if (Math.Abs(pos.Betrag - gesucht) < abstand)
                { abstand = Math.Abs(pos.Betrag - gesucht); treffer = pos.Betrag; }
            return treffer;
        }

        private static double Summe(List<KapitalwertRechner.InvestPosition> liste)
        {
            double s = 0;
            foreach (KapitalwertRechner.InvestPosition pos in liste) s += pos.Betrag;
            return s;
        }

        /// <summary>
        /// Der Kapitalwert eines Szenarios über den echten Rechenkern: die
        /// Investitionspositionen des Projekts nach der Vorrangregel, dazu feste Ansätze
        /// für Betrieb, Energie und Erlöse (mit dem Ertragsfaktor des Satzes) und die
        /// Zins-/Preisgrößen aus <see cref="WirtschaftlichkeitParameter.FuerSzenario"/>.
        /// </summary>
        private static double Kapitalwert(WirtschaftlichkeitParameter p, string szenario)
        {
            SzenarioSatz satz = p.SatzFuer(szenario);
            WirtschaftlichkeitParameter ps = p.FuerSzenario(szenario);

            double zuschuss;
            List<KapitalwertRechner.InvestPosition> invest = WirtschaftlichkeitCtrl.LiesInvestitionen(
                PROJEKT, szenario, satz, out zuschuss);

            double erloes = 900.0 * (satz != null ? satz.ErtragFaktor : 1.0);
            return KapitalwertRechner.Rechne(invest, 400.0, 3000.0, erloes,
                                             ps.Zinssatz, ps.Betrachtungszeitraum,
                                             ps.PreissteigerungBetrieb, ps.PreissteigerungEnergie,
                                             0, null, zuschuss).Kapitalwert;
        }
    }
}
