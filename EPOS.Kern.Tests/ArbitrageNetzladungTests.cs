using System;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die NETZLADUNG der Preissteuerung in der Bezugsreihe des Projekts</b>
    /// (Anwenderentscheid 17.09.2026 LS-E-4 (a), Befund LS-2).
    ///
    /// <para><b>Die Lage im Bestand.</b> Der Projektlauf zog von
    /// <c>Rest_Strombedarf_viertelstuendlich</c> nur die ENTLADUNG des Speichers ab.
    /// Für die Dauernutzung trägt das — sie lädt ausschließlich aus Überschuss. Die
    /// Preissteuerung darf im Graustrombetrieb aus dem NETZ laden, und diese Ladung
    /// liegt in einer getrennten Reihe (<see cref="ArbitrageErgebnis.LadungNetzAcKwh"/>).
    /// Sie erreichte den Projektlauf nicht: Netzbezug und Bezugsspitze waren
    /// untererfasst, und mit der Spitze auch der Leistungspreis.</para>
    ///
    /// <para><b>Was hier festgehalten wird.</b> Die Preissteuerung liefert jetzt
    /// dieselbe NETZWIRKUNG wie die Lastspitzenkappung — eine Reihe, die der
    /// Projektlauf ohne Kenntnis der Berechnungsart abzieht. Ihre Herleitung ist die
    /// Differenz der beiden Netzpfade (<c>Entladung − Netzladung</c>), weil die
    /// Preissteuerung die Netzladung getrennt führt; genau so bilanziert die Engine
    /// selbst.</para>
    ///
    /// <para>Ohne Datenbank: Die Fälle rechnen gegen die Engine und gegen
    /// <see cref="SimulationControl.SubVectors"/> — dieselbe Klemmung, die der
    /// Projektlauf anwendet.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class ArbitrageNetzladungTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        /// <summary>Viertelstundenraster des Rechenkerns [h].</summary>
        private const double DT = StromspeicherSimCtrl.INTERVALL_H;

        /// <summary>Grundlast der synthetischen Reihe [kW].</summary>
        private const double GRUNDLAST = 5.0;

        // =================================================================
        // 1 — Der Nachweisfall von Hand
        // =================================================================

        /// <summary>
        /// DER FALL AUS DEM AUFTRAG. Vier Viertelstunden, Grundlast 5 kW. In der
        /// zweiten lädt der Speicher 10 kWh aus dem Netz, in der vierten entlädt er
        /// 1 kWh.
        ///
        /// <para>Herleitung: 10 kWh in einer Viertelstunde sind 10 / 0,25 = 40 kW. Die
        /// Netzwirkung ist dort −40 kW, der Bezug also 5 + 40 = <b>45 kW</b> — und über
        /// die Viertelstunde 45 · 0,25 = 11,25 kWh statt 1,25 kWh, mithin genau
        /// +10 kWh. In der vierten Viertelstunde wirkt 1 kWh Entladung als +4 kW
        /// Wirkung: Der Bezug sinkt von 5 auf 1 kW wie bisher.</para>
        /// </summary>
        [Fact]
        public void Netzladung_von_10_kWh_hebt_den_Bezug_der_Viertelstunde_auf_45_kW()
        {
            double[] netzladung = { 0.0, 10.0, 0.0, 0.0 };
            double[] entladung = { 0.0, 0.0, 0.0, 1.0 };

            double[] wirkung = StromspeicherSimCtrl.NetzwirkungKw(Ergebnis(netzladung, entladung));

            Assert.Equal(4, wirkung.Length);
            Assert.Equal(0.0, wirkung[0]);
            Assert.Equal(-40.0, wirkung[1]);
            Assert.Equal(0.0, wirkung[2]);
            Assert.Equal(4.0, wirkung[3]);

            // Der Projektlauf: Rest − Netzwirkung, geklemmt bei 0.
            double[] rest = { GRUNDLAST, GRUNDLAST, GRUNDLAST, GRUNDLAST };
            double[] neu = new SimulationControl().SubVectors(rest, wirkung);

            Assert.Equal(5.0, neu[0]);
            Assert.Equal(45.0, neu[1]);     // 5 + 40 — die Spitze dieser Viertelstunde
            Assert.Equal(5.0, neu[2]);
            Assert.Equal(1.0, neu[3]);      // 5 − 4 — die Entladung senkt wie bisher

            // Die Jahresarbeit: +10 kWh Netzladung, −1 kWh Entladung.
            double vorher = 0.0, nachher = 0.0;
            for (int i = 0; i < rest.Length; i++) { vorher += rest[i] * DT; nachher += neu[i] * DT; }
            Assert.Equal(9.0, nachher - vorher, 9);

            // Und die Spitze steigt um genau die Netzladeleistung.
            Assert.Equal(45.0, Hoechstwert(neu));
            Assert.Equal(GRUNDLAST, Hoechstwert(rest));
        }

        /// <summary>
        /// GEGENPROBE zum Fall darüber: Ohne die Netzwirkung — also mit der blossen
        /// Entladung, wie es der Bestand tat — bleibt die Viertelstunde mit der
        /// Netzladung bei 5 kW. Das ist der Befund LS-2 in einer Zeile: Die Ladung
        /// verschwindet spurlos aus dem Netzbezug.
        /// </summary>
        [Fact]
        public void Gegenprobe_die_blosse_Entladung_uebersieht_die_Netzladung()
        {
            double[] netzladung = { 0.0, 10.0, 0.0, 0.0 };
            double[] entladung = { 0.0, 0.0, 0.0, 1.0 };
            ArbitrageErgebnis arb = Ergebnis(netzladung, entladung);

            double[] nurEntladung = StromspeicherSimCtrl.EntladungLeistungKw(arb.Ergebnis);
            double[] rest = { GRUNDLAST, GRUNDLAST, GRUNDLAST, GRUNDLAST };
            double[] alt = new SimulationControl().SubVectors(rest, nurEntladung);

            Assert.Equal(GRUNDLAST, alt[1]);            // die Netzladung fehlt
            Assert.Equal(GRUNDLAST, Hoechstwert(alt));  // und mit ihr die Spitze
        }

        // =================================================================
        // 2 — Die Herleitung trägt über den ganzen Lauf
        // =================================================================

        /// <summary>
        /// Über jedes Intervall gilt <c>Netzwirkung = (Entladung − Netzladung) / dt</c>,
        /// und negativ wird sie GENAU dort, wo aus dem Netz geladen wird — an einem
        /// echten Engine-Lauf mit Netzpfaden, nicht an gesetzten Reihen.
        /// </summary>
        [Fact]
        public void Am_Engine_Lauf_ist_die_Wirkung_genau_dort_negativ_wo_geladen_wird()
        {
            ArbitrageErgebnis arb = Graustromlauf();
            Assert.True(arb.Kennzahlen.LadungNetzKwh > 0.0,
                        "Der Lauf hat nicht aus dem Netz geladen — der Fall trägt nicht.");

            double[] wirkung = StromspeicherSimCtrl.NetzwirkungKw(arb);

            for (int i = 0; i < wirkung.Length; i++)
            {
                double erwartet = (arb.Ergebnis.EntladungAcKwh[i] - arb.LadungNetzAcKwh[i]) / DT;
                Assert.Equal(erwartet, wirkung[i]);

                if (arb.LadungNetzAcKwh[i] > 0.0 && arb.Ergebnis.EntladungAcKwh[i] == 0.0)
                    Assert.True(wirkung[i] < 0.0,
                                "Intervall " + i + " lädt aus dem Netz, die Wirkung ist nicht negativ.");
            }

            // Die Summe der Wirkung ist die Nettoentlastung des Netzes [kWh].
            double summe = 0.0;
            foreach (double w in wirkung) summe += w * DT;
            Assert.Equal(arb.Ergebnis.EntladeenergieKwh - arb.Kennzahlen.LadungNetzKwh, summe, 9);

            // UND der VERKAUF bleibt draussen: Er erhoeht die Einspeisung, nicht den
            // Bezug. Kein Verkaufsintervall darf die Bezugsreihe anfassen.
            Assert.True(arb.Kennzahlen.VerkaufKwh > 0.0,
                        "Der Lauf hat nichts verkauft — die Gegenprobe trägt nicht.");
            for (int i = 0; i < wirkung.Length; i++)
                if (arb.VerkaufAcKwh[i] > 0.0 && arb.LadungNetzAcKwh[i] == 0.0
                    && arb.Ergebnis.EntladungAcKwh[i] == 0.0)
                    Assert.Equal(0.0, wirkung[i]);
        }

        /// <summary>
        /// OHNE NETZLADUNG bleibt es Bit für Bit bei der Entladung — der reine
        /// Verkaufsbetrieb (Grünstrom) rechnet wie vor dem Befund. Das ist die Zusage,
        /// auf der die Byte-Gleichheit des Referenzlaufs ruht.
        /// </summary>
        [Fact]
        public void Ohne_Netzladung_bleibt_die_Wirkung_die_blosse_Entladung()
        {
            ArbitrageErgebnis arb = Verkaufslauf();
            Assert.Equal(0.0, arb.Kennzahlen.LadungNetzKwh);

            double[] wirkung = StromspeicherSimCtrl.NetzwirkungKw(arb);
            double[] entladung = StromspeicherSimCtrl.EntladungLeistungKw(arb.Ergebnis);

            Assert.Equal(entladung.Length, wirkung.Length);
            for (int i = 0; i < wirkung.Length; i++)
                Assert.Equal(entladung[i], wirkung[i]);   // exakt, nicht auf Stellen
        }

        /// <summary>
        /// Ohne Ergebnis gibt es keine Netzwirkung — der Weg wird benannt abgelehnt
        /// statt still einen Nullvektor zu liefern.
        /// </summary>
        [Fact]
        public void Ohne_Ergebnis_wird_die_Wirkung_benannt_abgelehnt()
        {
            Assert.Throws<ArgumentNullException>(
                () => StromspeicherSimCtrl.NetzwirkungKw((ArbitrageErgebnis)null));
        }

        // =================================================================
        // 3 — Der Projektlauf holt die Reihe ab
        // =================================================================

        /// <summary>
        /// DIE NAHT. Rechnet das Projekt mit der Preissteuerung, legt der Lauf seine
        /// Netzwirkung in den Kontext — dieselbe Reihe, die der Projektlauf für die
        /// Lastspitzenkappung abholt. Ohne diese Zeile erreicht die Netzladung
        /// <c>Rest_Strombedarf</c> nie, und der Befund LS-2 wäre nicht behoben.
        ///
        /// <para>Die Preisquelle des Projekts ist der Fixpreis; der Planer findet darin
        /// keine tragende Paarung, und die Wirkung ist deshalb die blosse Entladung.
        /// Das ist hier gerade richtig: Der Fall misst die NAHT, nicht die Höhe der
        /// Netzladung — die messen die Fälle darüber.</para>
        /// </summary>
        [Fact]
        public void Der_Arbitragelauf_legt_seine_Netzwirkung_in_den_Kontext()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Variante(DbWerte.SP_BERECHNUNG_ARBITRAGE, DbWerte.SP_BETRIEBSART_GRAUSTROM, true);

            var ctrl = new StromspeicherSimCtrl();
            SpeicherErgebnis ergebnis = ctrl.RechneAktiveVariante(Simulation(), PROJEKT);

            Assert.NotNull(ergebnis);
            StromspeicherLaufKontext k = ctrl.LetzterKontext;
            Assert.NotNull(k.Arbitrageergebnis);

            Assert.NotNull(k.NetzwirkungKw);
            Assert.Equal(35040, k.NetzwirkungKw.Length);

            double[] erwartet = StromspeicherSimCtrl.NetzwirkungKw(k.Arbitrageergebnis);
            for (int i = 0; i < erwartet.Length; i++) Assert.Equal(erwartet[i], k.NetzwirkungKw[i]);
        }

        /// <summary>
        /// GEGENPROBE: Die DAUERNUTZUNG lädt nur aus Überschuss und bekommt deshalb
        /// keine eigene Reihe — der Projektlauf zieht dort weiterhin die blosse
        /// Entladung ab, Bit für Bit wie zuvor.
        /// </summary>
        [Fact]
        public void Gegenprobe_die_Dauernutzung_bekommt_keine_Netzwirkung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Variante(DbWerte.SP_BERECHNUNG_DAUERNUTZUNG, DbWerte.SP_BETRIEBSART_GRAUSTROM, true);

            var ctrl = new StromspeicherSimCtrl();
            Assert.NotNull(ctrl.RechneAktiveVariante(Simulation(), PROJEKT));

            Assert.Null(ctrl.LetzterKontext.Arbitrageergebnis);
            Assert.Null(ctrl.LetzterKontext.NetzwirkungKw);
        }

        // =================================================================
        // Handgriffe
        // =================================================================

        /// <summary>„Beispiel WP WG 1" — führt genau eine Speicheranlage.</summary>
        private const int PROJEKT = 1026;

        /// <summary>Die aktive Speichervariante dieses Projekts (kleinste ID).</summary>
        private const int VARIANTE = 10;

        /// <summary>Setzt die aktive Variante auf Berechnungsart, Betriebsart und Netzentladung.</summary>
        private static void Variante(string art, string betriebsart, bool netzentladung)
        {
            DataRepository.ExecuteSQL(
                "UPDATE " + SchemaKatalog.TAB_STROMSPEICHERVARIANTE +
                " SET Berechnungsart = ?, Betriebsart = ?, Netzentladung = ? WHERE ID = ?",
                new DbParam("@art", art),
                new DbParam("@bart", betriebsart),
                new DbParam("@netz", netzentladung),
                new DbParam("@id", VARIANTE));
        }

        /// <summary>Eine Simulation mit synthetischer Stromreihe: nur Grundlast.</summary>
        private static SimulationControl Simulation()
        {
            double[] reihe = new double[35040];
            for (int i = 0; i < reihe.Length; i++) reihe[i] = GRUNDLAST;

            return new SimulationControl
            {
                simulation_Strombedarf = new SimulationStrombedarf
                {
                    Strombedarf_viertelStundenwerte = reihe
                },
                Rest_Strombedarf_viertelstuendlich = (double[])reihe.Clone()
            };
        }

        /// <summary>Höchstwert einer Reihe.</summary>
        private static double Hoechstwert(double[] reihe)
        {
            double max = double.NegativeInfinity;
            foreach (double w in reihe) if (w > max) max = w;
            return max;
        }

        /// <summary>
        /// Ein Arbitrage-Ergebnis mit genau diesen beiden Netzpfadreihen — alles
        /// andere trägt neutrale Werte. So steht jede Zahl des Nachweisfalls im Test
        /// selbst und nicht im Fahrplan des Planers.
        /// </summary>
        private static ArbitrageErgebnis Ergebnis(double[] netzladung, double[] entladung)
        {
            int n = netzladung.Length;
            var basis = new SpeicherErgebnis(
                new double[n], new double[n], 0.0, 0.0, Summe(entladung),
                SpeicherModus.Energetisch,
                new SpeicherEngine.WirtschaftlichkeitErgebnis(),
                null,
                new double[n],
                entladung);

            var plan = new ArbitragePlan(netzladung, new double[n], 0, 0, 0, 0, 0, 0.0, false, 0.0);

            return new ArbitrageErgebnis(basis, netzladung, new double[n], plan,
                                         new ArbitrageKennzahlen { LadungNetzKwh = Summe(netzladung) });
        }

        private static double Summe(double[] reihe)
        {
            double s = 0.0;
            foreach (double w in reihe) s += w;
            return s;
        }

        /// <summary>
        /// Ein echter Engine-Lauf im GRAUSTROMBETRIEB über einen Tag im
        /// Viertelstundenraster: billige Nacht, teurer Abend, leerer Speicher am Anfang.
        ///
        /// <para>Die Reihe trägt bewusst KEINE Last — nur so bleiben die Intervalle für
        /// den Planer frei (ein Intervall mit Eigenverbrauchsfluss ist weder Lade- noch
        /// Verkaufsslot, Fachkonzept 6.2). Der Planer paart dann die billige Netzladung
        /// mit dem teuren Verkauf, und beide Netzpfade stehen sauber getrennt in den
        /// Reihen — genau die Konstellation, um die es hier geht.</para>
        /// </summary>
        private static ArbitrageErgebnis Graustromlauf()
        {
            const int n = 96;
            double[] last = new double[n];
            double[] pv = new double[n];
            double[] netzpreis = new double[n];
            double[] erloes = new double[n];

            for (int i = 0; i < n; i++)
            {
                netzpreis[i] = i < n / 2 ? 2.0 : 40.0;    // Nacht billig, Abend teuer
                erloes[i] = i < n / 2 ? 1.0 : 38.0;
            }

            var eingang = new SpeicherEingang(last, pv,
                SpeicherEingang.KonstanteReihe(20.0, n), null).MitVerguetungen(5.0, 12.0);

            var optionen = new ArbitrageOptionen(netzpreis, erloes, true, true, 0.0, 0.0);

            return new Arbitrage(optionen).BerechneMitPlan(
                eingang, Parameter(SpeicherBetriebsart.Graustrom) with { StartSoCKwh = 0.0 });
        }

        /// <summary>
        /// Derselbe Lauf im GRÜNSTROMBETRIEB: Verkauf ja, Netzladung nein. Der
        /// Startfüllstand trägt den Verkauf.
        /// </summary>
        private static ArbitrageErgebnis Verkaufslauf()
        {
            const int n = 96;
            double[] last = new double[n];
            double[] pv = new double[n];
            double[] erloes = new double[n];

            for (int i = 0; i < n; i++)
            {
                last[i] = GRUNDLAST;
                pv[i] = i > n / 3 && i < n / 2 ? 20.0 : 0.0;   // ein Überschussfenster
                erloes[i] = i < n / 2 ? 1.0 : 38.0;
            }

            var eingang = new SpeicherEingang(last, pv,
                SpeicherEingang.KonstanteReihe(20.0, n), null).MitVerguetungen(5.0, 12.0);

            var optionen = new ArbitrageOptionen(
                SpeicherEingang.KonstanteReihe(2.0, n), erloes, false, true, 0.0, 0.0);

            return new Arbitrage(optionen).BerechneMitPlan(eingang, Parameter(SpeicherBetriebsart.Gruenstrom));
        }

        private static SpeicherParameter Parameter(SpeicherBetriebsart betriebsart) => new SpeicherParameter
        {
            CNomKwh = 20.0,
            PKw = 20.0,
            SoCMinKwh = 0.0,
            SoCMaxKwh = 20.0,
            RoundTripWirkungsgrad = 1.0,
            StartSoCKwh = 10.0,
            DtH = DT,
            VerguetungCtKwh = 5.0,
            CCapEurProKwh = 500.0,
            Kapitalzins = 0.03,
            NutzungsdauerA = 20.0,
            DegradationProA = 0.0,
            CVerEurProKwhZyklus = 0.0,
            Betriebsart = betriebsart
        };
    }
}
