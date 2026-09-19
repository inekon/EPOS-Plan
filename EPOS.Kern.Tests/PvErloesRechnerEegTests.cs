using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ETAPPE E1 — <b>die EEG-Seite des <see cref="PvErloesRechner"/></b>
    /// (Analysepapier 2026-09-19, Befund N3: „§ 51, Kappung, Marktprämie, § 51a
    /// ungetestet"). <c>PvStrangRechnungTests</c> deckt die Strangrechnung, also die
    /// Physik davor; hier geht es um die Vergütung.
    ///
    /// <para><b>Das Beispiel</b> ist das des Rechenwegs <c>06_Verguetungen_PV.md</c>:
    /// 300 kWp, Inbetriebnahme 01.08.2026, Überschusseinspeisung in der
    /// Direktvermarktung, 199.500 kWh Einspeisung, Jahresmarktwert 4,50 ct/kWh,
    /// Direktvermarktungsentgelt 0,40 ct/kWh, T = 20. Alle Zahlen des Papiers sind
    /// hier gemessen und getroffen.</para>
    ///
    /// <para>Rein — Katalog und Jahresmarktwert kommen als Delegat herein, die
    /// Stundenreihen sind synthetisch, keine Datenbank.</para>
    /// </summary>
    public class PvErloesRechnerEegTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static readonly CultureInfo DE = CultureInfo.GetCultureInfo("de-DE");

        private static readonly DateTime IBN_08_2026 = new DateTime(2026, 8, 1);

        private const double KWP = 300.0;
        private const double EINSPEISUNG_MWH = 199.5;       // 199.500 kWh
        private const double JAHRESMARKTWERT_CT = 4.50;
        private const double DV_CT = 0.40;
        private const int T = 20;

        /// <summary>Der anzulegende Wert dieser Anlage — 300 kWp ab 08/2026.</summary>
        private const double AW_CT = 6.04;

        private static Func<string, int, double?> Katalog()
        {
            IList<GesetzParameter> vor = GesetzKatalog.Vorbelegung();
            return (s, j) => vor.Where(p => p.Schluessel == s && p.JahrVon <= j)
                                .OrderByDescending(p => p.JahrVon)
                                .FirstOrDefault()?.Wert;
        }

        private static Func<int, double?> Jahresmarktwert() => j => JAHRESMARKTWERT_CT;

        private static ProjektPhotovoltaikModel Anlage(Action<ProjektPhotovoltaikModel> anpassen = null)
        {
            var pv = new ProjektPhotovoltaikModel
            {
                Aktiv = true,
                Vermarktungsform = DbWerte.PV_VERMARKTUNG_MARKTPRAEMIE,
                Einspeiseart = DbWerte.PV_EINSPEISEART_UEBERSCHUSS,
                Inbetriebnahme = IBN_08_2026,
                DvEntgelt = DV_CT,
                Par51_Anwenden = DbWerte.PV_SCHALTER_AUTO,
                Par51a_Kompensieren = true,
                Kappung60_Anwenden = DbWerte.PV_SCHALTER_AUTO,
            };
            anpassen?.Invoke(pv);
            return pv;
        }

        private static PvErloesErgebnis Rechne(ProjektPhotovoltaikModel pv,
                                               double kwp = KWP,
                                               double[] stunden = null,
                                               double[] spot = null)
        {
            return PvErloesRechner.Rechne(pv, kwp, EINSPEISUNG_MWH, stunden, spot, T,
                                          Katalog(), Jahresmarktwert(), DE);
        }

        // =====================================================================
        //  1 — Die Marktprämie
        // =====================================================================

        /// <summary>
        /// Der Beleg des Rechenwegs: Spot 7.182,00 € + Prämie 2.457,84 €
        /// − Direktvermarktungsentgelt 638,40 € = <b>9.001,44 €/a</b>.
        ///
        /// <para>Gerechnet auf der ARBEIT, also der Einspeisung abzüglich der
        /// Ausfallarbeit: 199.500 − 39.900 = 159.600 kWh.</para>
        /// </summary>
        [Fact]
        public void Die_Marktpraemie_des_Beispiels_ergibt_9001_44_Euro_im_ersten_Jahr()
        {
            PvErloesErgebnis r = Rechne(Anlage());

            const double ARBEIT = 159600.0;
            double spot = ARBEIT * JAHRESMARKTWERT_CT / 100.0;                 // 7.182,00
            double praemie = ARBEIT * (AW_CT - JAHRESMARKTWERT_CT) / 100.0;    // 2.457,84
            double dv = ARBEIT * DV_CT / 100.0;                                //   638,40

            Assert.Equal(7182.00, spot, 2);
            Assert.Equal(2457.84, praemie, 2);
            Assert.Equal(638.40, dv, 2);

            Assert.Equal(AW_CT, r.AwMixCt, 2);
            Assert.Equal(praemie, r.MarktpraemieEurJahr1, 2);
            Assert.Equal(9001.44, r.JeJahr[1], 2);
            Assert.Equal(spot + praemie - dv, r.JeJahr[1], 2);
        }

        /// <summary>
        /// Die Prämie ist <c>max(0, AW − JW)</c>: Liegt der Marktwert ÜBER dem
        /// anzulegenden Wert, gibt es keine Prämie — aber auch keinen Abzug.
        /// </summary>
        [Fact]
        public void Die_Praemie_wird_nie_negativ()
        {
            PvErloesErgebnis r = PvErloesRechner.Rechne(
                Anlage(), KWP, EINSPEISUNG_MWH, null, null, T,
                Katalog(), j => 9.00, DE);     // Marktwert über dem AW

            Assert.Equal(0.0, r.MarktpraemieEurJahr1, 2);
        }

        // =====================================================================
        //  2 — § 51 (negative Preise), Schalter AUTO
        // =====================================================================

        /// <summary>
        /// AUTO, Regel 1: Anlagen mit Inbetriebnahme VOR dem 25.02.2025 fallen nicht
        /// unter § 51 — es gibt keine Ausfallarbeit.
        /// </summary>
        [Fact]
        public void Paragraf_51_greift_nicht_bei_Inbetriebnahme_vor_dem_Stichtag()
        {
            Assert.Equal(new DateTime(2025, 2, 25), PvErloesRechner.Par51Stichtag);

            PvErloesErgebnis r = Rechne(Anlage(p => p.Inbetriebnahme = new DateTime(2025, 2, 24)));

            Assert.False(r.Par51Angewendet);
            Assert.Equal(0.0, r.VerguetungsausfallKwh, 6);
        }

        /// <summary>AUTO, Regel 2: ab 100 kWp sofort — unabhängig vom iMSys.</summary>
        [Fact]
        public void Paragraf_51_greift_ab_100_kWp_sofort()
        {
            PvErloesErgebnis r = Rechne(Anlage());

            Assert.True(r.Par51Angewendet);
            Assert.Equal(20.0, r.AusfallanteilProzent, 6);
            Assert.Equal(EINSPEISUNG_MWH * 1000.0 * 0.20, r.VerguetungsausfallKwh, 2);
        }

        /// <summary>
        /// AUTO, Regel 3: unter 100 kWp erst ab dem Jahr NACH dem iMSys-Einbau.
        /// Inbetriebnahme 2026, iMSys 2028 — die Jahre 1 bis 3 (2026–2028) tragen die
        /// volle Vergütung, ab Jahr 4 (2029) greift § 51.
        /// </summary>
        [Fact]
        public void Paragraf_51_greift_unter_100_kWp_erst_im_Jahr_nach_dem_iMSys()
        {
            PvErloesErgebnis r = PvErloesRechner.Rechne(
                Anlage(p => p.IMSys_Einbaujahr = 2028), 50.0, 40.0, null, null, T,
                Katalog(), Jahresmarktwert(), DE);

            // Jahre 1..3 gleich, ab Jahr 4 kleiner — und dann wieder konstant.
            Assert.Equal(r.JeJahr[1], r.JeJahr[2], 6);
            Assert.Equal(r.JeJahr[1], r.JeJahr[3], 6);
            Assert.True(r.JeJahr[4] < r.JeJahr[3], "Ab dem Jahr nach dem iMSys fällt Vergütung aus.");
            Assert.Equal(r.JeJahr[4], r.JeJahr[5], 6);

            // 20 % Ausfall im betroffenen Jahr.
            Assert.Equal(0.8, r.JeJahr[4] / r.JeJahr[3], 6);
        }

        /// <summary>
        /// Die Schalter JA und NEIN übersteuern die AUTO-Regel in beide Richtungen —
        /// auch bei einer Anlage, die AUTO nie erfassen würde.
        /// </summary>
        [Theory]
        [InlineData("JA", true)]
        [InlineData("NEIN", false)]
        [InlineData("AUTO", false)]
        public void Die_Schalter_uebersteuern_die_Auto_Regel(string schalter, bool erwartet)
        {
            PvErloesErgebnis r = Rechne(Anlage(p =>
            {
                p.Inbetriebnahme = new DateTime(2024, 1, 1);   // vor dem Stichtag
                p.Par51_Anwenden = schalter;
            }));

            Assert.Equal(erwartet, r.Par51Angewendet);
            if (erwartet) Assert.True(r.VerguetungsausfallKwh > 0);
            else Assert.Equal(0.0, r.VerguetungsausfallKwh, 6);
        }

        // =====================================================================
        //  3 — Ausfallanteil: Pauschale gegen stundenscharf
        // =====================================================================

        /// <summary>
        /// Stufe 1 ohne Stundenreihe: Pauschale 20 %, oder der gepflegte Wert.
        /// Gekennzeichnet als NICHT gemessen — die Vorschau sagt, woher sie es weiß.
        /// </summary>
        [Theory]
        [InlineData(null, 20.0)]
        [InlineData(10.0, 10.0)]
        [InlineData(0.0, 0.0)]
        [InlineData(35.0, 35.0)]
        public void Ohne_Stundenreihe_gilt_die_Pauschale(double? gepflegt, double erwartetProzent)
        {
            PvErloesErgebnis r = Rechne(Anlage(p => p.AusfallanteilProzent = gepflegt));

            Assert.False(r.AusfallGemessen);
            Assert.Equal(erwartetProzent, r.AusfallanteilProzent, 6);
            Assert.Equal(EINSPEISUNG_MWH * 1000.0 * erwartetProzent / 100.0,
                         r.VerguetungsausfallKwh, 2);
        }

        /// <summary>
        /// Stufe 2 mit Stundenreihen: Der Anteil wird GEMESSEN als
        /// <c>Σ Einsp(Spot &lt; 0) / Σ Einsp</c> — und weicht deutlich von der
        /// Pauschale ab. Dieselben Reihen tragen die 60-%-Kappung.
        /// </summary>
        [Fact]
        public void Mit_Stundenreihen_wird_der_Ausfallanteil_gemessen()
        {
            double[] stunden = Stundenreihe();
            double[] spot = Spotreihe();

            PvErloesErgebnis r = Rechne(Anlage(), stunden: stunden, spot: spot);

            Assert.True(r.AusfallGemessen);

            // 200 Stunden negativ, je 199.500/8.760 kWh, gegen die Jahressumme.
            // Der AUSGEWIESENE Anteil ist auf zwei Nachkommastellen gerundet —
            // gerechnet wird mit dem vollen Quotienten.
            double negativ = Enumerable.Range(0, 8760).Where(h => spot[h] < 0).Sum(h => stunden[h]);
            double gesamt = stunden.Sum();
            Assert.Equal(negativ / gesamt * 100.0, r.AusfallanteilProzent, 2);
            Assert.Equal(2.05, r.AusfallanteilProzent, 2);

            // Die Messung liegt weit unter der Pauschale von 20 %.
            Assert.True(r.AusfallanteilProzent < 5.0);

            // Ohne Kappung bemisst sich die Ausfallarbeit an der vollen Einspeisung.
            Assert.Equal(4089.06, r.VerguetungsausfallKwh, 2);
            Assert.Equal(EINSPEISUNG_MWH * 1000.0 * negativ / gesamt, r.VerguetungsausfallKwh, 2);
        }

        // =====================================================================
        //  4 — Die 60-%-Kappung
        // =====================================================================

        /// <summary>
        /// <c>Verlust = Σ max(0, Einsp_h − 0,6 × kWp)</c>. Die Prüfreihe hat 100
        /// Stunden mit 250 kW über der Schwelle 0,6 × 300 = 180 kW — das sind
        /// 100 × 70 = 7.000 kWh.
        /// </summary>
        [Fact]
        public void Die_Kappung_summiert_was_ueber_60_Prozent_der_Leistung_liegt()
        {
            double[] stunden = Stundenreihe();

            PvErloesErgebnis r = Rechne(Anlage(p => p.Kappung60_Anwenden = DbWerte.PV_SCHALTER_JA),
                                        stunden: stunden, spot: Spotreihe());

            Assert.True(r.KappungAngewendet);
            Assert.Equal(7000.00, r.KappungsverlustKwh, 2);

            // Dieselbe Summe von Hand.
            double schwelle = 0.6 * KWP;
            Assert.Equal(stunden.Sum(h => Math.Max(0.0, h - schwelle)), r.KappungsverlustKwh, 2);
        }

        /// <summary>Ohne Stundenreihe ist die Kappung nicht messbar — dann ist sie 0.</summary>
        [Fact]
        public void Ohne_Stundenreihe_gibt_es_keine_Kappung()
        {
            PvErloesErgebnis r = Rechne(Anlage(p => p.Kappung60_Anwenden = DbWerte.PV_SCHALTER_JA));

            Assert.False(r.KappungAngewendet);
            Assert.Equal(0.0, r.KappungsverlustKwh, 6);
        }

        /// <summary>Der Schalter NEIN schaltet sie auch mit Reihen ab.</summary>
        [Fact]
        public void Der_Schalter_NEIN_schaltet_die_Kappung_ab()
        {
            PvErloesErgebnis r = Rechne(Anlage(p => p.Kappung60_Anwenden = DbWerte.PV_SCHALTER_NEIN),
                                        stunden: Stundenreihe(), spot: Spotreihe());

            Assert.False(r.KappungAngewendet);
            Assert.Equal(0.0, r.KappungsverlustKwh, 6);
        }

        // =====================================================================
        //  5 — § 51a im letzten Vergütungsjahr
        // =====================================================================

        /// <summary>
        /// <b>§ 51a — Kompensation im letzten Vergütungsjahr.</b> Der Beleg des
        /// Rechenwegs: 39.900 kWh Ausfallarbeit des ersten Jahres × 0,5 × 6,04 ct
        /// × Degradationsfaktor 0,909156 = <b>1.095,52 €</b>, aufgeschlagen auf
        /// Jahr 20: 5.946,77 + 1.095,52 = 7.042,29 €.
        ///
        /// <para><b>OFFENER BEFUND V‑2.</b> Gerechnet wird mit dem ANZULEGENDEN WERT
        /// (AW). Ob das der richtige Satz ist, ist offen — die Vorschrift spricht vom
        /// Vergütungssatz, und bei Direktvermarktung ist der nicht der AW. Hier wird
        /// das HEUTIGE Verhalten gepinnt, damit eine spätere Korrektur als Änderung
        /// sichtbar wird und nicht als Zufall durchläuft.</para>
        /// </summary>
        [Fact]
        public void Paragraf_51a_schlaegt_im_letzten_Jahr_die_halbe_Ausfallarbeit_auf()
        {
            // Das Beispiel des Rechenwegs rechnet MIT Eigenverbrauch (85.500 kWh zu
            // 0,288 €/kWh); nur so trifft das letzte Jahr die Zahl des Papiers, weil
            // die Degradation den Eigenverbrauch in Mehrbezug umschlägt.
            PvErloesErgebnis r = PvErloesRechner.Rechne(
                Anlage(p => p.Degradation = 0.5), KWP, EINSPEISUNG_MWH, null, null, T,
                Katalog(), Jahresmarktwert(), DE, 85500.0, 0.288);

            Assert.Equal(20, r.LetztesVerguetungsjahr);

            double faktor = PvErloesRechner.DegradationsFaktor(0.5, 20);
            Assert.Equal(0.909156, faktor, 6);

            // V-2: der Satz ist der AW.
            Assert.Equal(39900.0 * 0.5 * AW_CT / 100.0 * faktor, r.Kompensation51aEur, 2);
            Assert.Equal(1095.52, r.Kompensation51aEur, 2);

            // Der Aufschlag landet im LETZTEN Jahr, nicht verteilt.
            Assert.Equal(7042.29, r.JeJahr[20], 2);
            Assert.Equal(5946.78, r.JeJahr[20] - r.Kompensation51aEur, 2);
        }

        /// <summary>
        /// Ohne Degradation entfällt der Faktor — dann ist die Kompensation die
        /// nackte Formel <c>Ausfallarbeit_J1 × 0,5 × AW / 100</c>.
        /// </summary>
        [Fact]
        public void Ohne_Degradation_ist_die_Kompensation_die_nackte_Formel()
        {
            PvErloesErgebnis r = Rechne(Anlage());

            Assert.Equal(1.0, PvErloesRechner.DegradationsFaktor(0.0, 20), 6);
            Assert.Equal(39900.0 * 0.5 * AW_CT / 100.0, r.Kompensation51aEur, 2);
            Assert.Equal(1204.98, r.Kompensation51aEur, 2);
        }

        /// <summary>Abgeschaltet gibt es sie nicht — und das letzte Jahr bleibt nackt.</summary>
        [Fact]
        public void Ohne_Kompensationsschalter_gibt_es_keinen_Aufschlag()
        {
            PvErloesErgebnis r = Rechne(Anlage(p => p.Par51a_Kompensieren = false));

            Assert.Equal(0.0, r.Kompensation51aEur, 6);
            Assert.Equal(r.JeJahr[19], r.JeJahr[20], 2);
        }

        /// <summary>
        /// Fällt kein Vergütungsanteil aus (§ 51 greift nicht), gibt es auch nichts
        /// zu kompensieren.
        /// </summary>
        [Fact]
        public void Ohne_Ausfallarbeit_gibt_es_keine_Kompensation()
        {
            PvErloesErgebnis r = Rechne(Anlage(p => p.Par51_Anwenden = DbWerte.PV_SCHALTER_NEIN));

            Assert.Equal(0.0, r.VerguetungsausfallKwh, 6);
            Assert.Equal(0.0, r.Kompensation51aEur, 6);
        }

        // =====================================================================
        //  6 — Der Rechner ist gutmütig
        // =====================================================================

        /// <summary>Ohne Anlage oder mit unsinniger Laufzeit wird nicht gerechnet, sondern gesagt.</summary>
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Eine_unsinnige_Laufzeit_bleibt_unvollstaendig(int jahre)
        {
            PvErloesErgebnis r = PvErloesRechner.Rechne(
                Anlage(), KWP, EINSPEISUNG_MWH, null, null, jahre, Katalog(), Jahresmarktwert(), DE);

            Assert.True(r.Unvollstaendig);
        }

        [Fact]
        public void Ohne_Anlage_bleibt_das_Ergebnis_unvollstaendig()
        {
            PvErloesErgebnis r = PvErloesRechner.Rechne(
                null, KWP, EINSPEISUNG_MWH, null, null, T, Katalog(), Jahresmarktwert(), DE);

            Assert.True(r.Unvollstaendig);
        }

        // =====================================================================
        //  Die Prüfreihen
        // =====================================================================

        /// <summary>
        /// 8.760 Stunden gleichverteilter Einspeisung; die ersten 100 Stunden mit
        /// 250 kW — also 70 kW über der Kappungsschwelle 0,6 × 300 kWp.
        /// </summary>
        private static double[] Stundenreihe()
        {
            var r = new double[8760];
            for (int h = 0; h < 8760; h++) r[h] = EINSPEISUNG_MWH * 1000.0 / 8760.0;
            for (int h = 0; h < 100; h++) r[h] = 250.0;
            return r;
        }

        /// <summary>Spotpreise: 4,50 ct/kWh, in 200 Stunden negativ.</summary>
        private static double[] Spotreihe()
        {
            var r = new double[8760];
            for (int h = 0; h < 8760; h++) r[h] = JAHRESMARKTWERT_CT;
            for (int h = 1000; h < 1200; h++) r[h] = -1.0;
            return r;
        }
    }
}
