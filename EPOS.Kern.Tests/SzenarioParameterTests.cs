using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

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
    ///
    /// <para><b>ETAPPE E9a (vollständige Szenarioabdeckung V‑E, Schritte B, C, D):</b> je
    /// Größe — Betrachtungszeitraum, Mengenänderung, Arbeits-, Grund- und Leistungspreis der
    /// Träger, Einspeisevergütung PV und KWK, DV-Entgelt, PPA-Preis — die Nullsemantik der
    /// neuen Größen („leer = wie Erwartet", keine Vorgabe), die Wirkungsrichtung der Pflege,
    /// der Speicherweg über die Controller, die Regel „gepflegt heißt verschieden um mehr als
    /// 1e−9", die Kohärenz neben Staffel und Rollenmodell und die Werkzeug-Wache der
    /// Testdatenbank. Träger dieser Fälle ist das BHKW-Projekt 1030 mit seinem gespeicherten
    /// Lauf und seinen gepflegten Trägerpreisen (Strom 60, Erdgas 63).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class SzenarioParameterTests
    {
        private const int PROJEKT = 1040;

        // ETAPPE E9a — die Prüffälle der vollständigen Szenarioabdeckung.
        private const int PROJEKT_BHKW = 1030;
        private const int TRAEGER_STROM = 60;       // „Elektrische Energie", 0,25 €/kWh, 2.400 €/a
        private const int TRAEGER_ERDGAS = 63;      // „Erdgas E", 0,84 €/Nm³, 1.200 €/a
        private const string NAME_STROM = "Elektrische Energie";
        private const string NAME_ERDGAS = "Erdgas E";
        private const int PRUEFSTAND = 9001;        // synthetischer Stand ohne Zeile in Tab_Projekt

        private const string ERWARTET = WirtschaftlichkeitSzenario.ERWARTET;
        private const string BEST = WirtschaftlichkeitSzenario.BEST;
        private const string WORST = WirtschaftlichkeitSzenario.WORST;

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
        // ETAPPE E9a — vollständige Szenarioabdeckung (V‑E), Schritte B, C, D
        // =====================================================================
        //
        // Je Größe (Zeitraum, Menge, Arbeits-/Grund-/Leistungspreis, Einspeisevergütung,
        // Einspeisevergütung KWK, DV-Entgelt, PPA-Preis): NULL heißt „wie Erwartet" (keine
        // Vorgabe, E9a‑Q5 a), die Pflege wirkt in die erwartete Richtung, der Speicherweg der
        // Controller hält sie, gepflegt ist erst ein Wert, der sich um mehr als 1e−9 vom
        // Erwartungswert unterscheidet, und Erwartet bleibt zahlengleich. Dazu die Kohärenz
        // neben der Leistungspreis-Staffel (E9a‑Q3) und im Tarif-Rollenmodell (E9a‑Q7) und die
        // Werkzeug-Wache der Testdatenbank (18 Spalten, Schemastand 118).

        /// <summary>
        /// Die drei Schemaschritte führen zusammen <b>achtzehn Spalten</b>: B (116) vier am
        /// Parametersatz — der Zeitraum ganzzahlig und eine EIGENE Spalte, nicht die
        /// Nutzungsdaueränderung aus Schritt 71 —, C (117) sechs an der Projektübersteuerung
        /// der Träger, D (118) je vier am Parametersatz und an der PV-Vergütungszeile.
        /// </summary>
        [Fact]
        public void E9a_Die_Schritte_116_bis_118_fuehren_achtzehn_Spalten()
        {
            Assert.True(SchemaStand.Zielversion >= 118,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter 118.");

            var typ = new Dictionary<string, string>();
            foreach (SchemaSpalte s in E9aSpalten()) typ[s.Tabelle + "." + s.Name] = s.TypDefinition;
            Assert.Equal(18, E9aSpalten().Count);
            Assert.Equal(18, typ.Count);                                 // keine doppelt

            string pw = SchemaKatalog.TAB_PROJEKTWIRTSCHAFT + ".";
            Assert.Equal("LONG", typ[pw + SchemaKatalog.SPALTE_PW_SZEN_BEST_ZEITRAUM]);
            Assert.Equal("LONG", typ[pw + SchemaKatalog.SPALTE_PW_SZEN_WORST_ZEITRAUM]);
            Assert.Equal("DOUBLE", typ[pw + SchemaKatalog.SPALTE_PW_SZEN_BEST_MENGE]);
            Assert.Equal("DOUBLE", typ[pw + SchemaKatalog.SPALTE_PW_SZEN_WORST_MENGE]);
            Assert.DoesNotContain(pw + SchemaKatalog.SPALTE_PW_SZEN_BEST_DAUER, typ.Keys);
            Assert.DoesNotContain(pw + SchemaKatalog.SPALTE_PW_SZEN_WORST_DAUER, typ.Keys);

            foreach (SchemaSpalte s in SchemaKatalog.Schritt116_Szenariorahmen)
                Assert.Equal(SchemaKatalog.TAB_PROJEKTWIRTSCHAFT, s.Tabelle);
            foreach (SchemaSpalte s in SchemaKatalog.Schritt117_TraegerpreisSzenario)
            {
                Assert.Equal(SchemaKatalog.ENERGY_PROJECT_SETTINGS, s.Tabelle);
                Assert.Equal("DOUBLE", s.TypDefinition);
            }
            foreach (SchemaSpalte s in SchemaKatalog.Schritt118_ErloessatzWirtschaftlichkeit)
            {
                Assert.Equal(SchemaKatalog.TAB_PROJEKTWIRTSCHAFT, s.Tabelle);
                Assert.Equal("DOUBLE", s.TypDefinition);
            }
            foreach (SchemaSpalte s in SchemaKatalog.Schritt118_ErloessatzPhotovoltaik)
            {
                Assert.Equal(SchemaKatalog.TAB_PROJEKTPHOTOVOLTAIK, s.Tabelle);
                Assert.Equal("DOUBLE", s.TypDefinition);
            }
            Assert.Equal(4, SchemaKatalog.Schritt116_Szenariorahmen.Length);
            Assert.Equal(6, SchemaKatalog.Schritt117_TraegerpreisSzenario.Length);
            Assert.Equal(8, SchemaKatalog.Schritt118_ErloessatzSzenario.Count());
        }

        /// <summary>
        /// DIE WERKZEUG-WACHE: Die REPO-Datei der Testdatenbank (nicht die nachgezogene
        /// Arbeitskopie) steht auf Schemastand 118 oder später und trägt alle achtzehn
        /// Spalten — und keine davon ist gepflegt: Die Referenzprojekte rechnen ohne Pflege,
        /// der Referenzlauf bleibt byte-gleich. Gelesen wird schreibgeschützt und ohne Spuren
        /// (<c>mode=ro&amp;immutable=1</c>, Muster <see cref="TestdatenbankSchemastandWacheTests"/>).
        /// </summary>
        [Fact]
        public void E9a_Die_Repo_Testdatenbank_traegt_die_18_Spalten_auf_Stand_118()
        {
            string pfad = RepoTestdatenbank();
            if (pfad == null) return;                    // keine Datei - nichts zu pruefen
            LfsZeigerProbe.Sicherstellen(pfad);

            string uri = "file:" + pfad.Replace('\\', '/').Replace("?", "%3f") + "?mode=ro&immutable=1";
            using var c = new Microsoft.Data.Sqlite.SqliteConnection(
                new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder { DataSource = uri }.ToString());
            c.Open();

            long stand = Skalar(c, "SELECT SchemaVersion FROM Tab_Applikation LIMIT 1");
            Assert.True(stand >= 118, "Die Testdatenbank steht auf Schemastand " + stand + ", erwartet mindestens 118.");

            var fehlend = new List<string>();
            var gepflegt = new List<string>();
            foreach (SchemaSpalte s in E9aSpalten())
            {
                if (Skalar(c, "SELECT COUNT(*) FROM pragma_table_info($t) WHERE name = $s", s.Tabelle, s.Name) == 0)
                {
                    fehlend.Add(s.Tabelle + "." + s.Name);
                    continue;
                }
                if (Skalar(c, "SELECT COUNT(*) FROM [" + s.Tabelle + "] WHERE [" + s.Name + "] IS NOT NULL") > 0)
                    gepflegt.Add(s.Tabelle + "." + s.Name);
            }
            Assert.True(fehlend.Count == 0,
                "Der Testdatenbank fehlen Spalten der Schritte 116 bis 118: " + string.Join(", ", fehlend) +
                " — Werkzeug Testdatenbankschema ziehen.");
            Assert.True(gepflegt.Count == 0,
                "Die Testdatenbank trägt Szenariowerte (die Referenzprojekte rechnen ohne Pflege): " +
                string.Join(", ", gepflegt));
        }

        /// <summary>
        /// NULL HEISST „WIE ERWARTET" (E9a‑Q5, Lesart a): Die vier neuen Felder des
        /// Szenariosatzes haben — anders als die sieben aus W5‑B‑9 — KEINE Vorgabe. Ohne
        /// Pflege trägt die Kopie des Szenarios Zeitraum und Einspeisevergütungen des
        /// Projektsatzes, der Mengenfaktor ist genau 1,0.
        /// </summary>
        [Fact]
        public void E9a_Ohne_Pflege_rechnen_die_neuen_Groessen_wie_Erwartet()
        {
            var p = new WirtschaftlichkeitParameter
            {
                Zinssatz = 3.0, Betrachtungszeitraum = 20, PreissteigerungEnergie = 2.0,
                PreissteigerungBetrieb = 1.0, Einspeiseverguetung = 0.08, EinspeiseverguetungKWK = 0.05
            };

            foreach (string szenario in new[] { BEST, WORST })
            {
                SzenarioSatz s = p.SatzFuer(szenario);
                Assert.True(s.NurVorgaben);
                Assert.Null(s.Zeitraum);
                Assert.Null(s.Menge);
                Assert.Null(s.Einspeiseverguetung);
                Assert.Null(s.EinspeiseverguetungKwk);

                Assert.Equal(20, s.ZeitraumWirksam(20));
                Assert.Equal(0.0, s.MengeWirksam);
                Assert.Equal(1.0, s.MengeFaktor);                       // genau 1,0: nichts zu skalieren
                Assert.Equal(0.08, s.EinspeiseverguetungWirksam(0.08));
                Assert.Equal(0.05, s.EinspeiseverguetungKwkWirksam(0.05));
                Assert.Null(s.EinspeiseverguetungKwkWirksam(null));

                WirtschaftlichkeitParameter k = p.FuerSzenario(szenario);
                Assert.Equal(20, k.Betrachtungszeitraum);
                Assert.Equal(0.08, k.Einspeiseverguetung);
                Assert.Equal(0.05, k.EinspeiseverguetungKWK);
            }

            // Die sieben Größen aus W5‑B‑9 behalten ihre Vorgaben.
            Assert.Equal(2.0, p.FuerSzenario(BEST).Zinssatz, 9);
            Assert.Equal(4.0, p.FuerSzenario(WORST).Zinssatz, 9);
            Assert.False(WirtschaftlichkeitCtrl.ZeitraumJeSzenario(p));
        }

        /// <summary>
        /// DIE REGEL „GEPFLEGT" (Konzept § 2.11.5): ein Wert steht da, ist nicht 0 und
        /// unterscheidet sich um MEHR als 1e−9 vom Erwartungswert. Dieselbe Regel gilt für den
        /// Zeitraum (ganze Jahre ab 1), die Mengenänderung, die Einspeisevergütungen, die
        /// Trägerpreise (<see cref="TraegerpreisSzenario.Wirksam"/>) und die PV-Erlössätze.
        /// </summary>
        [Fact]
        public void E9a_Gepflegt_heisst_verschieden_um_mehr_als_1e9()
        {
            Assert.Equal(1e-9, SzenarioSatz.GEPFLEGT_SCHWELLE);
            Assert.False(SzenarioSatz.Gepflegt(null, 0.08));
            Assert.False(SzenarioSatz.Gepflegt(0.0, 0.08));             // 0 heißt leer
            Assert.False(SzenarioSatz.Gepflegt(0.08, 0.08));
            Assert.False(SzenarioSatz.Gepflegt(0.08 + 5e-10, 0.08));
            Assert.False(SzenarioSatz.Gepflegt(0.08 - 5e-10, 0.08));
            Assert.True(SzenarioSatz.Gepflegt(0.08 + 2e-9, 0.08));
            Assert.True(SzenarioSatz.Gepflegt(-5.0, 0.0));               // eine Minderung ist Pflege

            SzenarioSatz s = SzenarioSatz.Vorgabe(BEST);
            s.Zeitraum = 20;
            Assert.False(s.ZeitraumGepflegt(20));
            s.Zeitraum = 0;
            Assert.False(s.ZeitraumGepflegt(20));
            s.Zeitraum = -3;
            Assert.Equal(20, s.ZeitraumWirksam(20));
            s.Zeitraum = 25;
            Assert.True(s.ZeitraumGepflegt(20));
            Assert.Equal(25, s.ZeitraumWirksam(20));

            s.Menge = 5e-10;
            Assert.False(s.MengeGepflegt);
            Assert.Equal(1.0, s.MengeFaktor);
            s.Einspeiseverguetung = 0.08 + 5e-10;
            Assert.Equal(0.08, s.EinspeiseverguetungWirksam(0.08));
            s.EinspeiseverguetungKwk = 0.0;
            Assert.Equal(0.05, s.EinspeiseverguetungKwkWirksam(0.05));

            bool gepflegt;
            Assert.Equal(0.25, TraegerpreisSzenario.Wirksam(0.25, null, out gepflegt));
            Assert.False(gepflegt);
            Assert.Equal(0.25, TraegerpreisSzenario.Wirksam(0.25, 0.0, out gepflegt));
            Assert.False(gepflegt);
            Assert.Equal(0.25, TraegerpreisSzenario.Wirksam(0.25, 0.25 + 5e-10, out gepflegt));
            Assert.False(gepflegt);
            Assert.Equal(0.20, TraegerpreisSzenario.Wirksam(0.25, 0.20, out gepflegt));
            Assert.True(gepflegt);
            Assert.Equal(40.0, TraegerpreisSzenario.Wirksam(null, 40.0, out gepflegt));
            Assert.True(gepflegt);

            var tp = new TraegerpreisSzenario { ArbeitspreisBest = 0.0, GrundpreisWorst = 0.0 };
            Assert.True(tp.Leer);
            tp.LeistungspreisWorst = 12.0;
            Assert.False(tp.Leer);
            Assert.Null(tp.Leistungspreis(ERWARTET));
            Assert.Null(tp.Leistungspreis(BEST));
            Assert.Equal(12.0, tp.Leistungspreis(WORST));
        }

        /// <summary>
        /// Gepflegt wirkt es: Die Kopie des Szenarios trägt den gepflegten Zeitraum und die
        /// gepflegten Einspeisevergütungen, jede Größe für sich; das Original bleibt der
        /// Erwartungsfall, und ERWARTET bekommt weiter dieselbe Referenz.
        /// </summary>
        [Fact]
        public void E9a_FuerSzenario_traegt_Zeitraum_und_Einspeiseverguetungen()
        {
            var p = new WirtschaftlichkeitParameter
            { Zinssatz = 3.0, Betrachtungszeitraum = 20, Einspeiseverguetung = 0.08, EinspeiseverguetungKWK = 0.05 };
            p.SatzBest.Zeitraum = 25;
            p.SatzBest.Einspeiseverguetung = 0.10;
            p.SatzWorst.Zeitraum = 15;
            p.SatzWorst.EinspeiseverguetungKwk = 0.03;

            Assert.Same(p, p.FuerSzenario(ERWARTET));

            WirtschaftlichkeitParameter best = p.FuerSzenario(BEST);
            Assert.Equal(25, best.Betrachtungszeitraum);
            Assert.Equal(0.10, best.Einspeiseverguetung);
            Assert.Equal(0.05, best.EinspeiseverguetungKWK);

            WirtschaftlichkeitParameter worst = p.FuerSzenario(WORST);
            Assert.Equal(15, worst.Betrachtungszeitraum);
            Assert.Equal(0.08, worst.Einspeiseverguetung);
            Assert.Equal(0.03, worst.EinspeiseverguetungKWK);

            Assert.Equal(20, p.Betrachtungszeitraum);                    // das Original bleibt
            Assert.Equal(0.08, p.Einspeiseverguetung);
            Assert.Equal(0.05, p.EinspeiseverguetungKWK);
            Assert.False(p.SatzBest.NurVorgaben);
            Assert.True(WirtschaftlichkeitCtrl.ZeitraumJeSzenario(p));
        }

        /// <summary>Der Mengenfaktor ist 1 + Menge/100 und nie negativ.</summary>
        [Fact]
        public void E9a_Der_Mengenfaktor_ist_eins_plus_Menge_und_nie_negativ()
        {
            SzenarioSatz s = SzenarioSatz.Vorgabe(WORST);
            Assert.Equal(1.0, s.MengeFaktor);
            s.Menge = 10.0;
            Assert.Equal(1.1, s.MengeFaktor, 12);
            s.Menge = -10.0;
            Assert.Equal(0.9, s.MengeFaktor, 12);
            s.Menge = -150.0;
            Assert.Equal(0.0, s.MengeFaktor);
            Assert.Equal(-150.0, s.MengeWirksam);
        }

        /// <summary>
        /// DIE EINE STELLE DES MENGENFAKTORS (E9a‑Q2, Lesart a): <see cref="SzenarioMengen"/>
        /// skaliert auf einer KOPIE die Energiemengen des Ergebnisbaums, die
        /// Vollbenutzungsstunden, die Energiereihen samt Kanalreihen und die Bezugsspitze —
        /// nicht die Leistungen, Prozentwerte und Füllstände. Ohne Pflege (f = 1,0) kommt
        /// dieselbe Referenz zurück; das Original bleibt unberührt.
        /// </summary>
        [Fact]
        public void E9a_SzenarioMengen_skaliert_Mengen_und_Lastgang_nicht_Leistungen()
        {
            var erg = new ErgebnisModel
            {
                Energiebedarf = new ErgebnisEnergiebedarfModel
                { Waermebedarf_Gesamt = 1000.0, Waermelast_Max = 400.0, Strombedarf_Gesamt = 300.0, Stromrestbedarf = 120.0 },
                BHKW = new ErgebnisBHKWModel
                { Stromproduktion = 180.0, Gasverbrauch = 520.0, VbhElektrisch = 4000.0, Waermebedarfsdeckung = 60.0 },
                Photovoltaik = new ErgebnisPhotovoltaikModel
                { Stromproduktion = 50.0, Ueberschuss = 20.0, MaxSolareLeistung = 45.0 }
            };
            var reihen = new ZeitreihenSatz();
            reihen.Reihen[ZeitreihenSatz.NETZBEZUG] = Konstant(2.0);
            reihen.Reihen[ZeitreihenSatz.BedarfSchluessel(0)] = Konstant(3.0);
            reihen.Reihen["PUFFER_1"] = Konstant(55.0);                  // Füllstand: bleibt
            reihen.Bezugsspitze = new Netzbezugsspitze { JahrKW = 80.0 };
            reihen.Bezugsspitze.MonatKW[0] = 70.0;
            var v = new VariantenDaten { IdProjekt = PRUEFSTAND, Ergebnis = erg, Zeitreihen = reihen, Energiekosten = 1234.0 };

            Assert.Same(v, SzenarioMengen.Variante(v, 1.0));

            VariantenDaten k = SzenarioMengen.Variante(v, 1.1);
            Assert.NotSame(v, k);
            Assert.Equal(1100.0, k.Ergebnis.Energiebedarf.Waermebedarf_Gesamt, 9);
            Assert.Equal(330.0, k.Ergebnis.Energiebedarf.Strombedarf_Gesamt, 9);
            Assert.Equal(132.0, k.Ergebnis.Energiebedarf.Stromrestbedarf, 9);
            Assert.Equal(400.0, k.Ergebnis.Energiebedarf.Waermelast_Max);   // Leistung
            Assert.Equal(198.0, k.Ergebnis.BHKW.Stromproduktion, 9);
            Assert.Equal(572.0, k.Ergebnis.BHKW.Gasverbrauch, 9);
            Assert.Equal(4400.0, k.Ergebnis.BHKW.VbhElektrisch, 9);
            Assert.Equal(60.0, k.Ergebnis.BHKW.Waermebedarfsdeckung);     // Prozent
            Assert.Equal(22.0, k.Ergebnis.Photovoltaik.Ueberschuss, 9);
            Assert.Equal(45.0, k.Ergebnis.Photovoltaik.MaxSolareLeistung);
            Assert.Equal(2.2, k.Zeitreihen.Reihen[ZeitreihenSatz.NETZBEZUG][100], 12);
            Assert.Equal(3.3, k.Zeitreihen.Reihen[ZeitreihenSatz.BedarfSchluessel(0)][5], 12);
            Assert.Same(reihen.Reihen["PUFFER_1"], k.Zeitreihen.Reihen["PUFFER_1"]);
            Assert.Equal(88.0, k.Zeitreihen.Bezugsspitze.JahrKW, 9);
            Assert.Equal(77.0, k.Zeitreihen.Bezugsspitze.MonatKW[0], 9);

            // Das Original bleibt der Erwartungsfall; bepreist wird die Kopie erst vom Aufrufer.
            Assert.Equal(1000.0, v.Ergebnis.Energiebedarf.Waermebedarf_Gesamt);
            Assert.Equal(2.0, v.Zeitreihen.Reihen[ZeitreihenSatz.NETZBEZUG][100]);
            Assert.Equal(80.0, v.Zeitreihen.Bezugsspitze.JahrKW);
            Assert.Equal(1234.0, k.Energiekosten);
        }

        /// <summary>
        /// Die Nachweiszeile je Szenario nennt IMMER den Zeitraum und die Einspeisevergütung,
        /// die Vergütung KWK nur, wo eine geführt wird, und die Mengenänderung nur, wenn sie
        /// gepflegt ist — „wie Erwartet" wird nicht wiederholt.
        /// </summary>
        [Fact]
        public void E9a_Die_Nachweiszeile_nennt_Zeitraum_Verguetung_und_nur_gepflegte_Mengen()
        {
            var p = new WirtschaftlichkeitParameter { Zinssatz = 3.0, Betrachtungszeitraum = 20, Einspeiseverguetung = 0.082 };

            string ohne = p.SatzFuer(BEST).Nachweis(p, DE);
            Assert.Contains("T = 20 a", ohne);
            Assert.Contains("Einspeisevergütung 0,082 €/kWh", ohne);
            Assert.DoesNotContain("KWK", ohne);
            Assert.DoesNotContain("Mengen", ohne);

            p.EinspeiseverguetungKWK = 0.05;
            p.SatzBest.Zeitraum = 25;
            p.SatzBest.Menge = -7.5;
            p.SatzBest.EinspeiseverguetungKwk = 0.04;
            string mit = p.SatzFuer(BEST).Nachweis(p, DE);
            Assert.Contains("T = 25 a", mit);
            Assert.Contains("Einspeisevergütung 0,082 €/kWh", mit);
            Assert.Contains("Einspeisevergütung KWK 0,040 €/kWh", mit);
            Assert.Contains("Mengen -7,5 %", mit);

            string worst = p.SatzFuer(WORST).Nachweis(p, DE);
            Assert.Contains("T = 20 a", worst);
            Assert.Contains("Einspeisevergütung KWK 0,050 €/kWh", worst);
            Assert.DoesNotContain("Mengen", worst);
        }

        /// <summary>
        /// STUFE 0 DER FORMELMAPPE (Norm 9 c): Der Parameterblock nennt je Szenario den
        /// Zeitraum, die Mengenänderung und beide Einspeisevergütungen — die WIRKSAMEN Werte,
        /// ohne Pflege die des Erwartungsfalls. Ohne gepflegte Trägerpreise bleibt der Block
        /// <c>PARAMETERBLOCK_ZEILEN</c> hoch.
        /// </summary>
        [Fact]
        public void E9a_Der_Parameterblock_nennt_Zeitraum_Menge_und_Verguetungen_je_Szenario()
        {
            var p = new WirtschaftlichkeitParameter
            { Zinssatz = 3.0, Betrachtungszeitraum = 20, Einspeiseverguetung = 0.08, EinspeiseverguetungKWK = 0.05 };
            p.SatzBest.Zeitraum = 25;
            p.SatzWorst.Menge = -10.0;
            p.SatzBest.Einspeiseverguetung = 0.10;
            p.SatzWorst.EinspeiseverguetungKwk = 0.03;

            using var wb = new XLWorkbook();
            IXLWorksheet ws = wb.AddWorksheet("Wirtschaftlichkeit");
            int naechste = ExcelFormelmappe.Parameterblock(ws, 1, p, null);

            Assert.Equal(1 + ExcelFormelmappe.PARAMETERBLOCK_ZEILEN, naechste);
            Blockzeile(ws, 3, R.WIRT_FM_PARAM_ZEITRAUM, 20, 25, 20);
            Blockzeile(ws, 10, R.WIRT_FM_PARAM_MENGE, 0.0, 0.0, -0.10);
            Blockzeile(ws, 11, R.WIRT_FM_PARAM_VERGUETUNG, 0.08, 0.10, 0.08);
            Blockzeile(ws, 12, R.WIRT_FM_PARAM_VERGUETUNG_KWK, 0.05, 0.05, 0.03);
            Assert.Equal(R.WIRT_FM_PARAM_HINWEIS, ws.Cell(13, 1).GetString());
            Assert.Equal(R.WIRT_FM_GRENZE, ws.Cell(14, 1).GetString());
        }

        /// <summary>
        /// DV-ENTGELT JE SZENARIO (Schritt D): <see cref="ProjektPhotovoltaikCtrl.FuerSzenario"/>
        /// ersetzt das Entgelt, wo es gepflegt ist; die Vergütungsreihe folgt Zahl für Zahl.
        /// Das Beispiel ist das der <see cref="PvErloesRechnerEegTests"/> (300 kWp, 199,5 MWh
        /// Einspeisung, 20 % Ausfall → 159.600 kWh vergütete Arbeit, 9.001,44 € im ersten
        /// Jahr): 0,20 ct weniger Entgelt sind 319,20 € mehr, 0,20 ct mehr ebenso viel weniger.
        /// Ohne Pflege, mit 0 und mit dem Erwartungswert bleibt es dieselbe Referenz.
        /// </summary>
        [Fact]
        public void E9a_DvEntgelt_je_Szenario_wirkt_auf_die_Verguetungsreihe()
        {
            ProjektPhotovoltaikModel pv = PvAnlage(DbWerte.PV_VERMARKTUNG_MARKTPRAEMIE);
            Assert.Same(pv, ProjektPhotovoltaikCtrl.FuerSzenario(pv, BEST));
            pv.DvEntgeltBest = 0.40;                    // gleich Erwartet: keine Pflege
            pv.DvEntgeltWorst = 0.0;                    // 0 heißt leer
            Assert.Same(pv, ProjektPhotovoltaikCtrl.FuerSzenario(pv, BEST));
            Assert.Same(pv, ProjektPhotovoltaikCtrl.FuerSzenario(pv, WORST));

            pv.DvEntgeltBest = 0.20;
            pv.DvEntgeltWorst = 0.60;
            Assert.Same(pv, ProjektPhotovoltaikCtrl.FuerSzenario(pv, ERWARTET));
            ProjektPhotovoltaikModel best = ProjektPhotovoltaikCtrl.FuerSzenario(pv, BEST);
            ProjektPhotovoltaikModel worst = ProjektPhotovoltaikCtrl.FuerSzenario(pv, WORST);
            Assert.NotSame(pv, best);
            Assert.Equal(0.20, best.DvEntgelt);
            Assert.Equal(0.60, worst.DvEntgelt);
            Assert.Equal(0.40, pv.DvEntgelt);           // das Original bleibt

            double erwartet = PvErloes(pv).JeJahr[1];
            Assert.Equal(9001.44, erwartet, 2);
            Assert.Equal(erwartet + 319.20, PvErloes(best).JeJahr[1], 2);
            Assert.Equal(erwartet - 319.20, PvErloes(worst).JeJahr[1], 2);
        }

        /// <summary>
        /// PPA-PREIS JE SZENARIO (Schritt D): Bei sonstiger Direktvermarktung mit Festpreis ist
        /// der Erlös des ersten Jahres Arbeit × PPA-Preis — der Szenariopreis ersetzt ihn, das
        /// DV-Entgelt bleibt stehen.
        /// </summary>
        [Fact]
        public void E9a_PpaPreis_je_Szenario_wirkt_auf_die_Verguetungsreihe()
        {
            ProjektPhotovoltaikModel pv = PvAnlage(DbWerte.PV_VERMARKTUNG_SONSTIGE_DV);
            pv.PpaPreis = 7.0;
            Assert.Same(pv, ProjektPhotovoltaikCtrl.FuerSzenario(pv, WORST));
            pv.PpaPreisBest = 8.0;
            pv.PpaPreisWorst = 5.5;

            ProjektPhotovoltaikModel best = ProjektPhotovoltaikCtrl.FuerSzenario(pv, BEST);
            ProjektPhotovoltaikModel worst = ProjektPhotovoltaikCtrl.FuerSzenario(pv, WORST);
            Assert.Equal(8.0, best.PpaPreis);
            Assert.Equal(5.5, worst.PpaPreis);
            Assert.Equal(7.0, pv.PpaPreis);
            Assert.Equal(0.40, best.DvEntgelt);

            double erwartet = PvErloes(pv).JeJahr[1];
            double arbeitKwh = erwartet / 0.07;         // Erlös = Arbeit × 7 ct/kWh
            Assert.True(arbeitKwh > 0, "Keine vergütete Arbeit im Prüfbeispiel.");
            Assert.Equal(arbeitKwh * 0.08, PvErloes(best).JeJahr[1], 2);
            Assert.Equal(arbeitKwh * 0.055, PvErloes(worst).JeJahr[1], 2);
        }

        /// <summary>
        /// DER SPEICHERWEG DES PARAMETERSATZES (Schritte B und D): Zeitraum, Mengenänderung
        /// und beide Einspeisevergütungen je Szenario gehen über die
        /// <see cref="WirtschaftlichkeitCtrl"/> hin und zurück — beim Anlegen der Zeile und
        /// beim Überschreiben; <c>null</c> bleibt <c>null</c>, eine 0 kommt als leer zurück
        /// („NULL/0 heißt wie Erwartet").
        /// </summary>
        [Fact]
        public void E9a_Rahmen_und_Erloessaetze_ueberleben_Speichern_und_Laden()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            foreach (SchemaSpalte s in SchemaKatalog.Schritt116_Szenariorahmen.Concat(SchemaKatalog.Schritt118_ErloessatzWirtschaftlichkeit))
                Assert.True(DataRepository.SpalteVorhanden(s.Tabelle, s.Name), "Spalte fehlt: " + s.Tabelle + "." + s.Name);

            var ctrl = new WirtschaftlichkeitCtrl();
            WirtschaftlichkeitParameter p = ctrl.LadeParameter(PROJEKT);
            Assert.True(p.SatzBest.NurVorgaben);          // frisch migriert: alles NULL
            Assert.True(p.SatzWorst.NurVorgaben);

            p.SatzBest.Zeitraum = 25;
            p.SatzWorst.Zeitraum = 15;
            p.SatzBest.Menge = 7.5;
            p.SatzWorst.Menge = -12.0;
            p.SatzBest.Einspeiseverguetung = 0.095;
            p.SatzWorst.Einspeiseverguetung = 0.0;        // 0 = wie Erwartet
            p.SatzWorst.EinspeiseverguetungKwk = 0.031;
            Assert.True(ctrl.SpeichereParameter(p));

            WirtschaftlichkeitParameter z = new WirtschaftlichkeitCtrl().LadeParameter(PROJEKT);
            Assert.Equal(25, z.SatzBest.Zeitraum);
            Assert.Equal(15, z.SatzWorst.Zeitraum);
            Assert.Equal(7.5, z.SatzBest.Menge);
            Assert.Equal(-12.0, z.SatzWorst.Menge);
            Assert.Equal(0.095, z.SatzBest.Einspeiseverguetung);
            Assert.Null(z.SatzWorst.Einspeiseverguetung);
            Assert.Null(z.SatzBest.EinspeiseverguetungKwk);
            Assert.Equal(0.031, z.SatzWorst.EinspeiseverguetungKwk);
            Assert.Null(z.SatzBest.Zinssatz);              // die W5‑B‑9-Felder bleiben leer
            Assert.False(z.SatzBest.NurVorgaben);

            // Überschreiben: ein Wert geht, einer kommt, einer wird leer.
            z.SatzBest.Zeitraum = null;
            z.SatzBest.EinspeiseverguetungKwk = 0.06;
            z.SatzWorst.Menge = null;
            Assert.True(new WirtschaftlichkeitCtrl().SpeichereParameter(z));

            WirtschaftlichkeitParameter z2 = new WirtschaftlichkeitCtrl().LadeParameter(PROJEKT);
            Assert.Null(z2.SatzBest.Zeitraum);
            Assert.Equal(15, z2.SatzWorst.Zeitraum);
            Assert.Equal(0.06, z2.SatzBest.EinspeiseverguetungKwk);
            Assert.Null(z2.SatzWorst.Menge);
            Assert.Equal(7.5, z2.SatzBest.Menge);
        }

        /// <summary>
        /// DER SPEICHERWEG DER TRÄGERKARTE (Schritt C): Die sechs Szenariopreise eines Trägers
        /// gehen über <see cref="EnergietraegerPreisCtrl.SzenarioSchreiben"/> hin und über
        /// <see cref="EnergietraegerPreisCtrl.SzenarioLesen"/> und den Leseweg der Karte zurück;
        /// eine 0 wird leer, der Erwartet-Preis bleibt. Die Vorprobe des Szenariolaufs und die
        /// Liste des Berichts sehen genau die gepflegten Träger.
        /// </summary>
        [Fact]
        public void E9a_Traegerpreise_ueberleben_Speichern_und_Laden()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.True(EnergietraegerPreisCtrl.SzenarioSpaltenVorhanden());
            Assert.True(EnergietraegerPreisCtrl.SzenarioLesen(PROJEKT_BHKW, TRAEGER_ERDGAS).Leer);
            Assert.False(EnergietraegerPreisCtrl.SzenarioGepflegt(PROJEKT_BHKW, BEST));
            Assert.Empty(EnergietraegerPreisCtrl.SzenarioJeTraeger(PROJEKT_BHKW));

            var satz = new TraegerpreisSzenario
            { ArbeitspreisBest = 0.74, ArbeitspreisWorst = 0.0, GrundpreisWorst = 1500.0, LeistungspreisBest = 12.5 };
            Assert.True(EnergietraegerPreisCtrl.SzenarioSchreiben(PROJEKT_BHKW, TRAEGER_ERDGAS, satz));

            TraegerpreisSzenario z = EnergietraegerPreisCtrl.SzenarioLesen(PROJEKT_BHKW, TRAEGER_ERDGAS);
            Assert.Equal(0.74, z.ArbeitspreisBest);
            Assert.Null(z.ArbeitspreisWorst);             // 0 wird leer
            Assert.Null(z.GrundpreisBest);
            Assert.Equal(1500.0, z.GrundpreisWorst);
            Assert.Equal(12.5, z.LeistungspreisBest);
            Assert.Null(z.LeistungspreisWorst);

            EnergietraegerPreisCtrl.Projektpreis karte = EnergietraegerPreisCtrl.ProjektpreisLesen(PROJEKT_BHKW, TRAEGER_ERDGAS);
            Assert.Equal(0.74, karte.Szenario.ArbeitspreisBest);
            Assert.Equal(1500.0, karte.Szenario.GrundpreisWorst);
            Assert.Equal(0.84, karte.Arbeitspreis);       // der Erwartet-Preis bleibt
            Assert.Equal(1200.0, karte.Grundpreis);

            Assert.True(EnergietraegerPreisCtrl.SzenarioGepflegt(PROJEKT_BHKW, BEST));
            Assert.True(EnergietraegerPreisCtrl.SzenarioGepflegt(PROJEKT_BHKW, WORST));
            Assert.False(EnergietraegerPreisCtrl.SzenarioGepflegt(PROJEKT_BHKW, ERWARTET));
            KeyValuePair<int, TraegerpreisSzenario> eintrag = Assert.Single(EnergietraegerPreisCtrl.SzenarioJeTraeger(PROJEKT_BHKW));
            Assert.Equal(TRAEGER_ERDGAS, eintrag.Key);

            // Ein Träger ohne Zeile im Projekt wird nicht getroffen; Leeren setzt alles zurück.
            Assert.False(EnergietraegerPreisCtrl.SzenarioSchreiben(PROJEKT_BHKW, 999999, satz));
            Assert.True(EnergietraegerPreisCtrl.SzenarioSchreiben(PROJEKT_BHKW, TRAEGER_ERDGAS, new TraegerpreisSzenario()));
            Assert.True(EnergietraegerPreisCtrl.SzenarioLesen(PROJEKT_BHKW, TRAEGER_ERDGAS).Leer);
            Assert.False(EnergietraegerPreisCtrl.SzenarioGepflegt(PROJEKT_BHKW, BEST));
        }

        /// <summary>
        /// DER SPEICHERWEG DER PV-VERGÜTUNGSZEILE (Schritt D): DV-Entgelt und PPA-Preis je
        /// Szenario gehen über <see cref="ProjektPhotovoltaikCtrl.Speichern"/> hin — beim
        /// Anlegen und beim Überschreiben — und über <see cref="ProjektPhotovoltaikCtrl.Lies"/>
        /// zurück; eine 0 wird leer, die Kopie trägt die vier Felder mit.
        /// </summary>
        [Fact]
        public void E9a_PV_Erloessaetze_ueberleben_Speichern_und_Laden()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.True(ProjektPhotovoltaikCtrl.SzenarioSpaltenVorhanden());
            var ctrl = new ProjektPhotovoltaikCtrl();
            ProjektPhotovoltaikModel m = ctrl.LiesOderVorbelegt(PROJEKT);
            m.Aktiv = true;
            m.Vermarktungsform = DbWerte.PV_VERMARKTUNG_SONSTIGE_DV;
            m.PpaPreis = 7.0;
            m.PpaPreisBest = 8.0;
            m.PpaPreisWorst = 0.0;                        // 0 = wie Erwartet
            m.DvEntgeltWorst = 0.55;
            Assert.True(ctrl.Speichern(m));

            ProjektPhotovoltaikModel z = new ProjektPhotovoltaikCtrl().Lies(PROJEKT);
            Assert.NotNull(z);
            Assert.Equal(8.0, z.PpaPreisBest);
            Assert.Null(z.PpaPreisWorst);
            Assert.Null(z.DvEntgeltBest);
            Assert.Equal(0.55, z.DvEntgeltWorst);
            Assert.Equal(7.0, z.PpaPreis);

            z.PpaPreisBest = null;
            z.PpaPreisWorst = 5.5;
            Assert.True(ctrl.Speichern(z));
            ProjektPhotovoltaikModel z2 = new ProjektPhotovoltaikCtrl().Lies(PROJEKT);
            Assert.Null(z2.PpaPreisBest);
            Assert.Equal(5.5, z2.PpaPreisWorst);
            Assert.Equal(0.55, z2.DvEntgeltWorst);

            ProjektPhotovoltaikModel kopie = ProjektPhotovoltaikCtrl.Kopie(z2);
            Assert.Equal(5.5, kopie.PpaPreisWorst);
            Assert.Equal(0.55, kopie.DvEntgeltWorst);
        }

        /// <summary>
        /// ZEITRAUM JE SZENARIO (Schritt B, E9a‑Q4 Lesart a): Ein Szenario mit eigenem
        /// Zeitraum rechnet <b>exakt wie ein Lauf mit diesem Zeitraum</b> — Horizont, Restwert
        /// am Ende von T_s, Ersatzbeschaffungen innerhalb T_s. Günstig mit T + 5 ist Zahl für
        /// Zahl das Günstig-Szenario eines Laufs mit T + 5, Ungünstig mit T − 5 das eines Laufs
        /// mit T − 5; Erwartet bleibt zahlengleich. Projekt 1030 mit seinem gespeicherten Lauf.
        /// </summary>
        [Fact]
        public void E9a_Der_Zeitraum_je_Szenario_rechnet_wie_ein_Lauf_mit_diesem_Zeitraum()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            var ctrl = new WirtschaftlichkeitCtrl();

            WirtschaftlichkeitParameter ohne = ctrl.LadeParameter(PROJEKT_BHKW);
            int t = ohne.Betrachtungszeitraum;
            Assert.True(t > 6, "Betrachtungszeitraum " + t + " zu kurz für den Prüffall.");
            Dictionary<string, WirtschaftlichkeitErgebnis> basis = Szenarien1030(ohne);

            WirtschaftlichkeitParameter mit = ctrl.LadeParameter(PROJEKT_BHKW);
            mit.SatzBest.Zeitraum = t + 5;
            mit.SatzWorst.Zeitraum = t - 5;
            Dictionary<string, WirtschaftlichkeitErgebnis> gepflegt = Szenarien1030(mit);

            WirtschaftlichkeitParameter lang = ctrl.LadeParameter(PROJEKT_BHKW);
            lang.Betrachtungszeitraum = t + 5;
            WirtschaftlichkeitParameter kurz = ctrl.LadeParameter(PROJEKT_BHKW);
            kurz.Betrachtungszeitraum = t - 5;

            Assert.Equal(Kw(Szenarien1030(lang)[BEST]), Kw(gepflegt[BEST]), 6);
            Assert.Equal(Kw(Szenarien1030(kurz)[WORST]), Kw(gepflegt[WORST]), 6);
            Assert.Equal(Kw(basis[ERWARTET]), Kw(gepflegt[ERWARTET]), 9);
            Assert.NotEqual(Kw(basis[BEST]), Kw(gepflegt[BEST]));
            Assert.NotEqual(Kw(basis[WORST]), Kw(gepflegt[WORST]));
        }

        /// <summary>
        /// DER VERLAUF JE SZENARIO ÜBER SEINEN ZEITRAUM: Jede Linie endet dort, wo ihr
        /// Kapitalwert steht (Ungünstig über T_Worst, Erwartet über T, Günstig über T_Best);
        /// ohne gepflegten Zeitraum ist es Zahl für Zahl der gemeinsame Horizont.
        /// </summary>
        [Fact]
        public void E9a_Der_Verlauf_laeuft_je_Szenario_ueber_seinen_Zeitraum()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            var ctrl = new WirtschaftlichkeitCtrl();

            WirtschaftlichkeitParameter p = ctrl.LadeParameter(PROJEKT_BHKW);
            int t = p.Betrachtungszeitraum;
            p.SatzBest.Zeitraum = t + 5;
            p.SatzWorst.Zeitraum = t - 5;
            Assert.True(WirtschaftlichkeitCtrl.ZeitraumJeSzenario(p));

            WirtschaftlichkeitVerlaufSzenarien drei = ctrl.BerechneVerlaufSzenarienJeZeitraum(Gruppe1030(), p);
            Assert.Equal(t + 5, drei.Jahre);
            Assert.Equal(t + 5, drei.Lauf(BEST).Jahre);
            Assert.Equal(t, drei.Lauf(ERWARTET).Jahre);
            Assert.Equal(t - 5, drei.Lauf(WORST).Jahre);
            Assert.Equal(t + 6, Linie(drei.Lauf(BEST)).Kumuliert.Length);
            Assert.Equal(t + 1, Linie(drei.Lauf(ERWARTET)).Kumuliert.Length);
            Assert.Equal(t - 4, Linie(drei.Lauf(WORST)).Kumuliert.Length);

            WirtschaftlichkeitParameter ohne = ctrl.LadeParameter(PROJEKT_BHKW);
            Assert.False(WirtschaftlichkeitCtrl.ZeitraumJeSzenario(ohne));
            WirtschaftlichkeitVerlaufSzenarien gleich = ctrl.BerechneVerlaufSzenarienJeZeitraum(Gruppe1030(), ohne);
            WirtschaftlichkeitVerlaufSzenarien gemeinsam = ctrl.BerechneVerlaufSzenarien(Gruppe1030(), ohne, t);
            Assert.Equal(t, gleich.Jahre);
            foreach (string s in WirtschaftlichkeitVerlaufSzenarien.Reihenfolge)
                Assert.Equal(Linie(gemeinsam.Lauf(s)).Kumuliert, Linie(gleich.Lauf(s)).Kumuliert);
        }

        /// <summary>
        /// MENGENÄNDERUNG JE SZENARIO (Schritt B, E9a‑Q2 Lesart a): +10 % im günstigen und
        /// −10 % im ungünstigen Szenario skalieren die Energiemengen des Laufs, BEVOR sie Preise
        /// treffen — die Energiekosten steigen bzw. fallen um denselben Betrag (die Grundpreise
        /// bleiben), die Rechnung ist in der Menge affin. Leer, 0 und ein Wert unter der
        /// Schwelle rechnen wie ohne Pflege; Erwartet bleibt zahlengleich.
        /// </summary>
        [Fact]
        public void E9a_Der_Mengenfaktor_skaliert_die_Energiekosten_um_die_Festbetraege()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            var ctrl = new WirtschaftlichkeitCtrl();

            Dictionary<string, WirtschaftlichkeitErgebnis> basis = Szenarien1030(ctrl.LadeParameter(PROJEKT_BHKW));
            double e0 = basis[ERWARTET].EnergiekostenJahr.Value;
            Assert.Equal(e0, basis[BEST].EnergiekostenJahr.Value, 9);   // ohne Pflege: die Mengen des Laufs

            WirtschaftlichkeitParameter p = ctrl.LadeParameter(PROJEKT_BHKW);
            p.SatzBest.Menge = 10.0;
            p.SatzWorst.Menge = -10.0;
            Dictionary<string, WirtschaftlichkeitErgebnis> gepflegt = Szenarien1030(p);
            double eb = gepflegt[BEST].EnergiekostenJahr.Value;
            double ew = gepflegt[WORST].EnergiekostenJahr.Value;

            Assert.True(eb > e0 && e0 > ew, "Energiekosten Best " + eb + ", Erwartet " + e0 + ", Worst " + ew);
            Nahe(2.0 * e0, eb + ew, 0.01, "Mengen ±10 % um die Festbeträge (affin)");
            Assert.True(eb - e0 < 0.1 * e0, "Die Grundpreise skalieren nicht mit.");
            Assert.Equal(e0, gepflegt[ERWARTET].EnergiekostenJahr.Value, 9);
            Assert.Equal(Kw(basis[ERWARTET]), Kw(gepflegt[ERWARTET]), 9);
            Assert.NotEqual(Kw(basis[BEST]), Kw(gepflegt[BEST]));

            // Leer, 0 und unter der Schwelle: wie ohne Pflege.
            WirtschaftlichkeitParameter leer = ctrl.LadeParameter(PROJEKT_BHKW);
            leer.SatzBest.Menge = 0.0;
            leer.SatzWorst.Menge = 5e-10;
            Dictionary<string, WirtschaftlichkeitErgebnis> gleich = Szenarien1030(leer);
            Assert.Equal(Kw(basis[BEST]), Kw(gleich[BEST]), 9);
            Assert.Equal(Kw(basis[WORST]), Kw(gleich[WORST]), 9);
        }

        /// <summary>
        /// ARBEITS- UND GRUNDPREIS JE SZENARIO (Schritt C, E9a‑Q3 Lesart a): Der Szenariopreis
        /// ersetzt den wirksamen Erwartet-Preis des Trägers als Ganzes. Erdgas mit ∓0,10 €/Nm³
        /// verschiebt die Energiekosten symmetrisch, der Strom-Grundpreis −400 €/a senkt sie im
        /// günstigen Szenario um genau 400 €. Ein Wert innerhalb der Schwelle ist keine Pflege.
        /// Erwartet bleibt zahlengleich; Nachweiszeile und Parameterblock nennen die gepflegten
        /// Preise.
        /// </summary>
        [Fact]
        public void E9a_Arbeits_und_Grundpreis_je_Szenario_ersetzen_den_Preis_als_Ganzes()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            var ctrl = new WirtschaftlichkeitCtrl();

            double? a0, g0, l0, sa0, sg0, sl0;
            KostenEmissionRechner.PreisSatz(PROJEKT_BHKW, TRAEGER_ERDGAS, ERWARTET, out a0, out g0, out l0);
            KostenEmissionRechner.PreisSatz(PROJEKT_BHKW, TRAEGER_STROM, ERWARTET, out sa0, out sg0, out sl0);
            Assert.True(a0.HasValue && a0.Value > 0.1, "Erdgas ohne Arbeitspreis.");
            Assert.True(sg0.HasValue && sg0.Value > 400.0, "Strom ohne Grundpreis.");

            Dictionary<string, WirtschaftlichkeitErgebnis> basis = Szenarien1030(ctrl.LadeParameter(PROJEKT_BHKW));
            double e0 = basis[ERWARTET].EnergiekostenJahr.Value;

            // Innerhalb der Schwelle: keine Pflege, keine andere Zahl.
            Assert.True(EnergietraegerPreisCtrl.SzenarioSchreiben(PROJEKT_BHKW, TRAEGER_ERDGAS,
                new TraegerpreisSzenario { ArbeitspreisBest = a0.Value + 5e-10 }));
            Dictionary<string, WirtschaftlichkeitErgebnis> schwelle = Szenarien1030(ctrl.LadeParameter(PROJEKT_BHKW));
            Assert.Equal(basis[BEST].EnergiekostenJahr.Value, schwelle[BEST].EnergiekostenJahr.Value, 9);
            Assert.Equal(Kw(basis[BEST]), Kw(schwelle[BEST]), 9);

            Assert.True(EnergietraegerPreisCtrl.SzenarioSchreiben(PROJEKT_BHKW, TRAEGER_ERDGAS,
                new TraegerpreisSzenario { ArbeitspreisBest = a0.Value - 0.10, ArbeitspreisWorst = a0.Value + 0.10 }));
            Assert.True(EnergietraegerPreisCtrl.SzenarioSchreiben(PROJEKT_BHKW, TRAEGER_STROM,
                new TraegerpreisSzenario { GrundpreisBest = sg0.Value - 400.0 }));

            Dictionary<string, WirtschaftlichkeitErgebnis> gepflegt = Szenarien1030(ctrl.LadeParameter(PROJEKT_BHKW));
            double eb = gepflegt[BEST].EnergiekostenJahr.Value;
            double ew = gepflegt[WORST].EnergiekostenJahr.Value;

            Assert.True(eb < e0 && ew > e0, "Energiekosten Best " + eb + ", Erwartet " + e0 + ", Worst " + ew);
            // Best = E0 − 0,10·M − 400, Worst = E0 + 0,10·M  ⇒  Best + (Worst − E0) = E0 − 400.
            Nahe(e0 - 400.0, eb + (ew - e0), 0.01, "Arbeitspreis ∓0,10 und Grundpreis −400");
            Assert.True(Kw(gepflegt[BEST]) > Kw(basis[BEST]));
            Assert.True(Kw(gepflegt[WORST]) < Kw(basis[WORST]));
            Assert.Equal(e0, gepflegt[ERWARTET].EnergiekostenJahr.Value, 9);
            Assert.Equal(Kw(basis[ERWARTET]), Kw(gepflegt[ERWARTET]), 9);

            // Die WIRKSAMEN Preise je Szenario — derselbe Leser wie die Energiekosten.
            double? ab, gb, lb;
            KostenEmissionRechner.PreisSatz(PROJEKT_BHKW, TRAEGER_ERDGAS, BEST, out ab, out gb, out lb);
            Assert.Equal(a0.Value - 0.10, ab.Value, 12);
            Assert.Equal(g0, gb);

            // Nachweis: nur, was gepflegt ist.
            VariantenDaten[] staende = { Stamm1030() };
            string best = TraegerpreisSzenario.Nachweiszeile(staende, BEST, DE);
            Assert.Contains(NAME_STROM + ": " + R.WIRT_SZ_TP_GRUND + " " + (sg0.Value - 400.0).ToString("N2", DE), best);
            Assert.Contains(NAME_ERDGAS + ": " + R.WIRT_SZ_TP_ARBEIT + " " + (a0.Value - 0.10).ToString("N4", DE), best);
            string worst = TraegerpreisSzenario.Nachweiszeile(staende, WORST, DE);
            Assert.Contains(NAME_ERDGAS + ": " + R.WIRT_SZ_TP_ARBEIT + " " + (a0.Value + 0.10).ToString("N4", DE), worst);
            Assert.DoesNotContain(NAME_STROM, worst);
            Assert.Null(TraegerpreisSzenario.Nachweiszeile(staende, ERWARTET, DE));

            // Parameterblock der Formelmappe: je Stand, Träger und Preisart eine Zeile.
            using var wb = new XLWorkbook();
            IXLWorksheet ws = wb.AddWorksheet("Wirtschaftlichkeit");
            int naechste = ExcelFormelmappe.Parameterblock(ws, 1, ctrl.LadeParameter(PROJEKT_BHKW), staende);
            Assert.Equal(1 + ExcelFormelmappe.PARAMETERBLOCK_ZEILEN + 2, naechste);
            Blockzeile(ws, 13, string.Format(R.WIRT_FM_PARAM_TP_GRUND, NAME_STROM, "Stamm"),
                       sg0.Value, sg0.Value - 400.0, sg0.Value);
            Blockzeile(ws, 14, string.Format(R.WIRT_FM_PARAM_TP_ARBEIT, NAME_ERDGAS, "Stamm"),
                       a0.Value, a0.Value - 0.10, a0.Value + 0.10);
            Assert.Equal(R.WIRT_FM_PARAM_HINWEIS, ws.Cell(15, 1).GetString());
        }

        /// <summary>
        /// LEISTUNGSPREIS JE SZENARIO OHNE STAFFEL (Schritt C): Der Szenariopreis ersetzt den
        /// Leistungspreis des Stromträgers; bemessen an der Bezugsspitze (150 kW, Jahres- und
        /// Monatssumme gleich gewählt, damit der Modus des Trägers keine Rolle spielt).
        /// </summary>
        [Fact]
        public void E9a_Der_Leistungspreis_je_Szenario_wirkt_ohne_Staffel()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            double? a, g, l0;
            KostenEmissionRechner.PreisSatz(PROJEKT_BHKW, TRAEGER_STROM, ERWARTET, out a, out g, out l0);
            Assert.True(EnergietraegerPreisCtrl.SzenarioSchreiben(PROJEKT_BHKW, TRAEGER_STROM,
                new TraegerpreisSzenario { LeistungspreisBest = 40.0, LeistungspreisWorst = 120.0 }));

            VariantenDaten v = Stamm1030(Spitze(150.0));
            VariantenDaten best = v.Kopie();
            KostenEmissionRechner.Berechne(best, BEST);
            VariantenDaten worst = v.Kopie();
            KostenEmissionRechner.Berechne(worst, WORST);

            double erwartetKw = l0 ?? 0.0;
            Assert.Equal(v.Energiekosten.Value + (40.0 - erwartetKw) * 150.0, best.Energiekosten.Value, 6);
            Assert.Equal(v.Energiekosten.Value + (120.0 - erwartetKw) * 150.0, worst.Energiekosten.Value, 6);
            Assert.Empty(best.SzenarioLeistungspreisOhneWirkung);
            Assert.True(best.SzenarioStrompreisGepflegt);
            Assert.False(v.SzenarioStrompreisGepflegt);
        }

        /// <summary>
        /// STAFFEL-KOHÄRENZ (E9a‑Q3, Lesart a): Ist am Stromträger eine Leistungspreis-Staffel
        /// gepflegt, gilt sie in ALLEN Szenarien — ein Szenario-Leistungspreis bleibt ohne
        /// Wirkung, und das Ergebnis des Szenarios sagt das in einer Kohärenzzeile (HINWEIS).
        /// Erwartet trägt keine solche Zeile.
        /// </summary>
        [Fact]
        public void E9a_Neben_der_Staffel_bleibt_der_Szenario_Leistungspreis_ohne_Wirkung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.True(EnergietraegerPreisCtrl.StaffelSchreiben(PROJEKT_BHKW, TRAEGER_STROM,
                new LeistungspreisStaffel { GrenzeKW = 100.0, Preis1EurKWa = 80.0, Preis2EurKWa = 110.0 }));
            Assert.True(EnergietraegerPreisCtrl.SzenarioSchreiben(PROJEKT_BHKW, TRAEGER_STROM,
                new TraegerpreisSzenario { LeistungspreisBest = 40.0 }));

            VariantenDaten v = Stamm1030(Spitze(150.0));
            VariantenDaten best = v.Kopie();
            KostenEmissionRechner.Berechne(best, BEST);

            // Die Staffel gilt in beiden: 100 kW × 80 + 50 kW × 110 = 13.500 €/a.
            Assert.Equal(v.Energiekosten.Value, best.Energiekosten.Value, 6);
            Assert.Equal(NAME_STROM, Assert.Single(best.SzenarioLeistungspreisOhneWirkung));
            Assert.Empty(v.SzenarioLeistungspreisOhneWirkung);

            string zeile = string.Format(R.WIRT_SZ_LEISTUNGSPREIS_OHNE_WIRKUNG, NAME_STROM);
            Dictionary<string, WirtschaftlichkeitErgebnis> je =
                Szenarien1030(new WirtschaftlichkeitCtrl().LadeParameter(PROJEKT_BHKW), Spitze(150.0));
            Assert.Contains(je[BEST].KohaerenzHinweise, h => h.Text == zeile && h.Schwere == KohaerenzSchwere.HINWEIS);
            Assert.DoesNotContain(je[ERWARTET].KohaerenzHinweise, h => h.Text == zeile);
            Assert.DoesNotContain(je[WORST].KohaerenzHinweise, h => h.Text == zeile);
            Assert.Equal(je[ERWARTET].EnergiekostenJahr.Value, je[BEST].EnergiekostenJahr.Value, 6);
        }

        /// <summary>
        /// EINSPEISEVERGÜTUNG PV UND KWK JE SZENARIO (Schritt D) auf einem synthetischen Stand
        /// (100 MWh PV-Überschuss, 17,52 MWh KWK-Einspeisung, ohne Vergütungsdialog): Der
        /// Einspeiseerlös folgt dem wirksamen Satz — im Verhältnis der Sätze, weil die
        /// Ertragsänderung des Szenarios in beiden Läufen dieselbe ist —, der Kapitalwert
        /// in die erwartete Richtung. Leer, 0 und innerhalb der Schwelle rechnen wie ohne
        /// Pflege; Erwartet bleibt zahlengleich.
        /// </summary>
        [Fact]
        public void E9a_Einspeiseverguetung_PV_und_KWK_je_Szenario_wirken_auf_den_Erloes()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Dictionary<string, WirtschaftlichkeitErgebnis> basis = Pruefstand(Pruefsatz());
            Assert.Equal(8000.0, basis[ERWARTET].EinspeiseerloesPvJahr, 6);      // 100 MWh × 0,08 €/kWh
            Assert.Equal(876.0, basis[ERWARTET].EinspeiseerloesKwkJahr, 6);      // 17,52 MWh × 0,05 €/kWh

            WirtschaftlichkeitParameter p = Pruefsatz();
            p.SatzBest.Einspeiseverguetung = 0.10;
            p.SatzWorst.Einspeiseverguetung = 0.06;
            p.SatzBest.EinspeiseverguetungKwk = 0.07;
            p.SatzWorst.EinspeiseverguetungKwk = 0.03;
            Dictionary<string, WirtschaftlichkeitErgebnis> gepflegt = Pruefstand(p);

            Assert.Equal(basis[BEST].EinspeiseerloesPvJahr * 0.10 / 0.08, gepflegt[BEST].EinspeiseerloesPvJahr, 6);
            Assert.Equal(basis[WORST].EinspeiseerloesPvJahr * 0.06 / 0.08, gepflegt[WORST].EinspeiseerloesPvJahr, 6);
            Assert.Equal(basis[BEST].EinspeiseerloesKwkJahr * 0.07 / 0.05, gepflegt[BEST].EinspeiseerloesKwkJahr, 6);
            Assert.Equal(basis[WORST].EinspeiseerloesKwkJahr * 0.03 / 0.05, gepflegt[WORST].EinspeiseerloesKwkJahr, 6);
            Assert.True(Kw(gepflegt[BEST]) > Kw(basis[BEST]));
            Assert.True(Kw(gepflegt[WORST]) < Kw(basis[WORST]));
            Assert.Equal(Kw(basis[ERWARTET]), Kw(gepflegt[ERWARTET]), 9);
            Assert.Equal(8000.0, gepflegt[ERWARTET].EinspeiseerloesPvJahr, 6);

            // Leer, 0 und innerhalb der Schwelle: wie ohne Pflege.
            WirtschaftlichkeitParameter leer = Pruefsatz();
            leer.SatzBest.Einspeiseverguetung = 0.08;
            leer.SatzWorst.Einspeiseverguetung = 0.0;
            leer.SatzBest.EinspeiseverguetungKwk = 0.05 + 5e-10;
            Dictionary<string, WirtschaftlichkeitErgebnis> gleich = Pruefstand(leer);
            Assert.Equal(Kw(basis[BEST]), Kw(gleich[BEST]), 9);
            Assert.Equal(Kw(basis[WORST]), Kw(gleich[WORST]), 9);
        }

        /// <summary>
        /// DAS TARIF-ROLLENMODELL (E9a‑Q7, Befund und Empfehlung): Im Rollenmodell ist der
        /// Erwartet-Strompreis der Reststromtarif, die Einspeisung bewertet der
        /// Einspeisetarif. Die Rollenpreise bleiben in allen Szenarien die Erwartet-Preise —
        /// ein Szenario-Strompreis kürzt sich heraus (Energiekosten − Flat-Netzanteil +
        /// Reststrom), eine Szenario-Einspeisevergütung trägt nichts bei. Beides wird benannt
        /// (Kohärenzzeilen), statt still verschluckt; Erwartet bleibt zahlengleich.
        /// </summary>
        [Fact]
        public void E9a_Im_Rollenmodell_bleiben_die_Rollenpreise_Erwartet()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            var ctrl = new WirtschaftlichkeitCtrl();

            var tarif = new TarifParameter { IdStamm = PROJEKT_BHKW, Aktiv = true, Modus = DbWerte.TARIF_MODUS_ROLLEN };
            tarif.Bezug.ArbeitspreisEurKWh = 0.32;
            tarif.Reststrom.ArbeitspreisEurKWh = 0.30;
            tarif.Einspeisung.ArbeitspreisEurKWh = 0.07;
            Assert.True(ctrl.SpeichereTarif(tarif));
            Assert.True(ctrl.LadeTarif(PROJEKT_BHKW).Wirksam);

            Dictionary<string, WirtschaftlichkeitErgebnis> basis =
                Szenarien1030(ctrl.LadeParameter(PROJEKT_BHKW), Stundenreihen1030());
            Assert.True(basis[ERWARTET].StromkostenTarif.HasValue, "Das Rollenmodell hat nicht gerechnet.");

            Assert.True(EnergietraegerPreisCtrl.SzenarioSchreiben(PROJEKT_BHKW, TRAEGER_STROM,
                new TraegerpreisSzenario { ArbeitspreisBest = 0.10, ArbeitspreisWorst = 0.40 }));
            WirtschaftlichkeitParameter p = ctrl.LadeParameter(PROJEKT_BHKW);
            p.SatzBest.Einspeiseverguetung = 0.12;
            Dictionary<string, WirtschaftlichkeitErgebnis> gepflegt = Szenarien1030(p, Stundenreihen1030());

            foreach (string s in new[] { BEST, WORST })
            {
                Assert.Equal(basis[s].EnergiekostenJahr.Value, gepflegt[s].EnergiekostenJahr.Value, 4);
                Assert.Equal(basis[s].StromkostenTarif.Value, gepflegt[s].StromkostenTarif.Value, 6);
                Assert.Equal(basis[s].EinspeiseerloesJahr, gepflegt[s].EinspeiseerloesJahr, 6);
                Assert.Contains(gepflegt[s].KohaerenzHinweise, h => h.Text == R.WIRT_SZ_ROLLEN_STROMPREIS);
                Assert.DoesNotContain(basis[s].KohaerenzHinweise, h => h.Text == R.WIRT_SZ_ROLLEN_STROMPREIS);
            }
            Assert.Contains(gepflegt[BEST].KohaerenzHinweise, h => h.Text == R.WIRT_SZ_ROLLEN_EINSPEISUNG);
            Assert.DoesNotContain(gepflegt[WORST].KohaerenzHinweise, h => h.Text == R.WIRT_SZ_ROLLEN_EINSPEISUNG);
            Assert.DoesNotContain(gepflegt[ERWARTET].KohaerenzHinweise, h => h.Text == R.WIRT_SZ_ROLLEN_STROMPREIS);
            Assert.Equal(Kw(basis[ERWARTET]), Kw(gepflegt[ERWARTET]), 9);
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

        // ---------------------------------------------------------------------
        // Hilfsmittel der Etappe E9a
        // ---------------------------------------------------------------------

        /// <summary>Die achtzehn Spalten der Schemaschritte 116 (B), 117 (C) und 118 (D).</summary>
        private static List<SchemaSpalte> E9aSpalten()
        {
            var liste = new List<SchemaSpalte>();
            liste.AddRange(SchemaKatalog.Schritt116_Szenariorahmen);
            liste.AddRange(SchemaKatalog.Schritt117_TraegerpreisSzenario);
            liste.AddRange(SchemaKatalog.Schritt118_ErloessatzSzenario);
            return liste;
        }

        /// <summary>
        /// Der Stamm 1030 mit seinem gespeicherten Lauf und seinen Erwartet-Energiekosten —
        /// derselbe Weg wie in <see cref="KwkAnlagenwahrheitTests"/>; wahlweise mit
        /// Stundenreihen oder einer Bezugsspitze.
        /// </summary>
        private static VariantenDaten Stamm1030(ZeitreihenSatz reihen = null)
        {
            var v = new VariantenDaten
            {
                IdProjekt = PROJEKT_BHKW,
                IstStamm = true,
                Projektname = "Prüffall E9a",
                Ergebnis = new ErgebnisCtrl().Load(PROJEKT_BHKW),
                Zeitreihen = reihen
            };
            Assert.NotNull(v.Ergebnis);
            KostenEmissionRechner.Berechne(v);
            Assert.True(v.Energiekosten.HasValue, "Projekt 1030 ohne Energiekosten: " + v.EnergiekostenGrund);
            return v;
        }

        /// <summary>Die Gruppe aus dem Stamm 1030 allein.</summary>
        private static BerichtsDaten Gruppe1030(ZeitreihenSatz reihen = null)
        {
            VariantenDaten v = Stamm1030(reihen);
            var daten = new BerichtsDaten { IdStamm = PROJEKT_BHKW, Stammprojektname = v.Projektname };
            daten.Varianten.Add(v);
            return daten;
        }

        /// <summary>Die drei Szenarioergebnisse des Stamms 1030 — gerechnet über
        /// <c>WirtschaftlichkeitCtrl.Berechne</c>, ohne zu speichern.</summary>
        private static Dictionary<string, WirtschaftlichkeitErgebnis> Szenarien1030(
            WirtschaftlichkeitParameter p, ZeitreihenSatz reihen = null)
        {
            var je = new Dictionary<string, WirtschaftlichkeitErgebnis>();
            foreach (WirtschaftlichkeitErgebnis e in new WirtschaftlichkeitCtrl().Berechne(Gruppe1030(reihen), p, 0, false))
                if (e.IdProjekt == PROJEKT_BHKW) je[e.Szenario] = e;
            Assert.Equal(3, je.Count);
            return je;
        }

        /// <summary>
        /// Der synthetische Prüfstand (ohne Zeile in <c>Tab_Projekt</c>, deshalb ohne
        /// Speichern — Muster <see cref="BerichtBlattstrukturWacheTests"/>): 100 MWh
        /// PV-Überschuss, feste Energiekosten, dazu die KWK-Reihen von <see cref="Kwkreihen"/>.
        /// </summary>
        private static Dictionary<string, WirtschaftlichkeitErgebnis> Pruefstand(WirtschaftlichkeitParameter p)
        {
            var v = new VariantenDaten
            {
                IdProjekt = PRUEFSTAND,
                IstStamm = true,
                Projektname = "Prüfstand E9a",
                Ergebnis = new ErgebnisModel
                {
                    Photovoltaik = new ErgebnisPhotovoltaikModel { Stromproduktion = 150.0, Ueberschuss = 100.0 }
                },
                Energiekosten = 10000.0,
                Zeitreihen = Kwkreihen()
            };
            var daten = new BerichtsDaten { IdStamm = PRUEFSTAND, Stammprojektname = v.Projektname };
            daten.Varianten.Add(v);

            var je = new Dictionary<string, WirtschaftlichkeitErgebnis>();
            foreach (WirtschaftlichkeitErgebnis e in new WirtschaftlichkeitCtrl().Berechne(daten, p, 0, false))
                je[e.Szenario] = e;
            Assert.Equal(3, je.Count);
            return je;
        }

        /// <summary>Der Parametersatz des Prüfstands: i = 3 %, T = 20 a, p = 0,
        /// Einspeisevergütung 0,08 €/kWh, KWK 0,05 €/kWh.</summary>
        private static WirtschaftlichkeitParameter Pruefsatz()
        {
            return new WirtschaftlichkeitParameter
            {
                IdStamm = PRUEFSTAND,
                IdReferenzprojekt = 0,
                Zinssatz = 3.0,
                Betrachtungszeitraum = 20,
                PreissteigerungEnergie = 0.0,
                PreissteigerungBetrieb = 0.0,
                Einspeiseverguetung = 0.08,
                EinspeiseverguetungKWK = 0.05
            };
        }

        /// <summary>1 kWh Bedarf und 3 kWh BHKW-Strom je Stunde, kein Netzbezug: 2 kWh
        /// werden eingespeist, 17,52 MWh im Jahr.</summary>
        private static ZeitreihenSatz Kwkreihen()
        {
            var z = new ZeitreihenSatz();
            z.Reihen[ZeitreihenSatz.STROMBEDARF] = Konstant(1.0);
            z.Reihen[ZeitreihenSatz.BHKW_STROM] = Konstant(3.0);
            z.Reihen[ZeitreihenSatz.NETZBEZUG] = Konstant(0.0);
            return z;
        }

        /// <summary>Flache Stundenreihen aus den Jahressummen des gespeicherten Laufs von
        /// 1030 — wortgleich zu den Konstanten der <see cref="KwkAnlagenwahrheitTests"/>.</summary>
        private static ZeitreihenSatz Stundenreihen1030()
        {
            const double BEDARF_MWH = 4790.09;
            const double BHKW_MWH = 432.3;
            int n = ZeitreihenSatz.Stunden;
            var z = new ZeitreihenSatz();
            z.Reihen[ZeitreihenSatz.STROMBEDARF] = Konstant(BEDARF_MWH * 1000.0 / n);
            z.Reihen[ZeitreihenSatz.BHKW_STROM] = Konstant(BHKW_MWH * 1000.0 / n);
            z.Reihen[ZeitreihenSatz.NETZBEZUG] = Konstant((BEDARF_MWH - BHKW_MWH) * 1000.0 / n);
            return z;
        }

        /// <summary>Nur eine Bezugsspitze: Jahresspitze und Summe der Monatsspitzen sind
        /// gleich, damit der Leistungspreis-Modus des Trägers (Jahr/Monat) keine Rolle spielt.</summary>
        private static ZeitreihenSatz Spitze(double kw)
        {
            var z = new ZeitreihenSatz { Bezugsspitze = new Netzbezugsspitze { JahrKW = kw } };
            for (int m = 0; m < z.Bezugsspitze.MonatKW.Length; m++) z.Bezugsspitze.MonatKW[m] = kw / 12.0;
            return z;
        }

        private static double[] Konstant(double wert)
        {
            var r = new double[ZeitreihenSatz.Stunden];
            for (int h = 0; h < r.Length; h++) r[h] = wert;
            return r;
        }

        /// <summary>Der Kapitalwert eines Ergebnisses — mit Grund, falls er fehlt.</summary>
        private static double Kw(WirtschaftlichkeitErgebnis e)
        {
            Assert.True(e.Kapitalwert.HasValue,
                        "Kein Kapitalwert im Szenario " + e.Szenario + ": " + (e.Fehlgrund ?? e.Hinweis));
            return e.Kapitalwert.Value;
        }

        /// <summary>Die Verlaufslinie des Stamms 1030 in einem Szenario.</summary>
        private static VerlaufSerie Linie(WirtschaftlichkeitVerlauf lauf)
        {
            Assert.NotNull(lauf);
            VerlaufSerie s = lauf.Absolut.Single(x => x.IdProjekt == PROJEKT_BHKW);
            Assert.True(s.Kumuliert != null, "Keine Verlaufslinie: " + s.Fehlgrund);
            return s;
        }

        /// <summary>Eine Zeile des Parameterblocks: Bezeichnung und die drei Szenariowerte.</summary>
        private static void Blockzeile(IXLWorksheet ws, int zeile, string titel,
                                       double erwartet, double guenstig, double unguenstig)
        {
            Assert.Equal(titel, ws.Cell(zeile, 1).GetString());
            Assert.Equal(erwartet, ws.Cell(zeile, 2).GetDouble(), 9);
            Assert.Equal(guenstig, ws.Cell(zeile, 3).GetDouble(), 9);
            Assert.Equal(unguenstig, ws.Cell(zeile, 4).GetDouble(), 9);
        }

        private static void Nahe(double erwartet, double ist, double toleranz, string was)
        {
            Assert.True(Math.Abs(erwartet - ist) <= toleranz,
                        was + ": erwartet " + erwartet.ToString("R", CultureInfo.InvariantCulture) +
                        ", ist " + ist.ToString("R", CultureInfo.InvariantCulture));
        }

        /// <summary>Die PV-Anlage des Beispiels der <see cref="PvErloesRechnerEegTests"/>:
        /// Inbetriebnahme 08/2026, Überschusseinspeisung, DV-Entgelt 0,40 ct/kWh.</summary>
        private static ProjektPhotovoltaikModel PvAnlage(string vermarktung)
        {
            return new ProjektPhotovoltaikModel
            {
                Aktiv = true,
                Vermarktungsform = vermarktung,
                Einspeiseart = DbWerte.PV_EINSPEISEART_UEBERSCHUSS,
                Inbetriebnahme = new DateTime(2026, 8, 1),
                DvEntgelt = 0.40,
                Par51_Anwenden = DbWerte.PV_SCHALTER_AUTO,
                Par51a_Kompensieren = true,
                Kappung60_Anwenden = DbWerte.PV_SCHALTER_AUTO
            };
        }

        /// <summary>Die Vergütungsreihe des Beispiels: 300 kWp, 199,5 MWh Einspeisung, T = 20,
        /// Jahresmarktwert 4,50 ct/kWh, Gesetzeskatalog aus der Vorbelegung.</summary>
        private static PvErloesErgebnis PvErloes(ProjektPhotovoltaikModel pv)
        {
            IList<GesetzParameter> vor = GesetzKatalog.Vorbelegung();
            Func<string, int, double?> katalog = (s, j) => vor.Where(x => x.Schluessel == s && x.JahrVon <= j)
                                                              .OrderByDescending(x => x.JahrVon)
                                                              .FirstOrDefault()?.Wert;
            return PvErloesRechner.Rechne(pv, 300.0, 199.5, null, null, 20, katalog, j => 4.50, DE);
        }

        /// <summary>
        /// <c>Referenzlaeufe/Kenndaten_Test.sqlite</c> unter der Repowurzel, <c>null</c> ohne
        /// Datei — gefunden wie in <see cref="TestdatenbankSchemastandWacheTests"/>: erst über
        /// den Pfad DIESER Quelldatei, sonst aufwärts vom Laufordner bis zu <c>WP-Plan.sln</c>.
        /// </summary>
        private static string RepoTestdatenbank(
            [System.Runtime.CompilerServices.CallerFilePath] string eigeneDatei = null)
        {
            string wurzel = null;
            if (!string.IsNullOrEmpty(eigeneDatei))
            {
                string ordner = Path.GetDirectoryName(eigeneDatei);
                string kandidat = ordner == null ? null : Path.GetDirectoryName(ordner);
                if (kandidat != null && File.Exists(Path.Combine(kandidat, "WP-Plan.sln"))) wurzel = kandidat;
            }
            if (wurzel == null)
            {
                DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
                while (d != null && wurzel == null)
                {
                    if (File.Exists(Path.Combine(d.FullName, "WP-Plan.sln"))) wurzel = d.FullName;
                    d = d.Parent;
                }
            }
            if (wurzel == null) return null;
            string datei = Path.Combine(wurzel, "Referenzlaeufe", "Kenndaten_Test.sqlite");
            return File.Exists(datei) ? datei : null;
        }

        /// <summary>Eine Zahl aus der schreibgeschützten Verbindung; <c>$t</c> und <c>$s</c>
        /// sind die optionalen Parameter (Tabelle, Spalte).</summary>
        private static long Skalar(Microsoft.Data.Sqlite.SqliteConnection c, string sql, params string[] werte)
        {
            using Microsoft.Data.Sqlite.SqliteCommand b = c.CreateCommand();
            b.CommandText = sql;
            if (werte.Length > 0) b.Parameters.AddWithValue("$t", werte[0]);
            if (werte.Length > 1) b.Parameters.AddWithValue("$s", werte[1]);
            object o = b.ExecuteScalar();
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt64(o, CultureInfo.InvariantCulture);
        }
    }
}
