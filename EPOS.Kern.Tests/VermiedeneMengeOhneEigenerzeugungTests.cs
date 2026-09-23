using System;
using System.Collections.Generic;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>ETAPPE E7 — Konzept § 6.3 Nr. 32 / Entscheid U6‑Q1 (22.09.2026): die
    /// vermiedene Bezugsmenge ohne jede Eigenerzeugung.</b>
    ///
    /// <para>Bezugsgröße des Ausweises ist der Strombedarf des Projekts ohne jede
    /// Eigenerzeugung — der Bedarf VOR Abzug der PV-Eigennutzung. Die vermiedene Menge
    /// führt damit KWK- und PV-Eigenverbrauch, die § 9b-Korrektur greift auf beide, der
    /// Verteilschlüssel bringt beide Anlagen ein, und der Leistungsanteil der
    /// Bezugsseite hängt am Lastbild des vollen Bedarfs. Unverändert bleiben der
    /// KWK-Eigenanteil (min-Regel auf den Bedarf NACH Photovoltaik), der Kapitalwert und
    /// jede Reihe.</para>
    ///
    /// <para><b>Der Weg ist der des Kerns</b> — Stundenreihen → <see cref="StromMatrix"/>
    /// → <see cref="StromTarifRechner"/> → Verteilschlüssel
    /// (<c>WirtschaftlichkeitCtrl.VermiedenAufteilung</c>). Die Reihen sind flach und
    /// tragen die Mengen des Mockup-Beispiels (Kategorie 7): Bedarf 1.429,7 MWh,
    /// Photovoltaik-Eigennutzung 85,5 MWh, BHKW 1.094,2 MWh, Restbezug 250,0 MWh.</para>
    /// </summary>
    public class VermiedeneMengeOhneEigenerzeugungTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        private static readonly CultureInfo DE = CultureInfo.GetCultureInfo("de-DE");

        private const double BEDARF_MWH = 1429.7;
        private const double PV_MWH = 85.5;
        private const double BHKW_MWH = 1094.2;
        private const double BEZUG_MWH = 250.0;
        private const double ARBEITSPREIS = 0.2880;     // €/kWh, Bezug wie Reststrom
        private const double SATZ_9B = 20.00;           // €/MWh

        private static ZeitreihenSatz Reihen(double bedarfMWh, double pvMWh, double bhkwMWh,
                                             double bezugMWh)
        {
            int n = ZeitreihenSatz.Stunden;
            var bedarf = new double[n];
            var pv = new double[n];
            var bhkw = new double[n];
            var bezug = new double[n];
            for (int h = 0; h < n; h++)
            {
                bedarf[h] = bedarfMWh * 1000.0 / n;
                pv[h] = pvMWh * 1000.0 / n;
                bhkw[h] = bhkwMWh * 1000.0 / n;
                bezug[h] = bezugMWh * 1000.0 / n;
            }
            var z = new ZeitreihenSatz();
            z.Reihen[ZeitreihenSatz.STROMBEDARF] = bedarf;
            if (pvMWh > 0) z.Reihen[ZeitreihenSatz.PV_GENUTZT] = pv;
            if (bhkwMWh > 0) z.Reihen[ZeitreihenSatz.BHKW_STROM] = bhkw;
            z.Reihen[ZeitreihenSatz.NETZBEZUG] = bezug;
            return z;
        }

        private static StromMatrix Matrix(double bedarf, double pv, double bhkw, double bezug)
        {
            StromMatrix m = StromMatrix.Baue(Reihen(bedarf, pv, bhkw, bezug), new TarifParameter());
            Assert.NotNull(m);
            return m;
        }

        private static TarifRolle Rolle(double monatspreis = 0)
        {
            return new TarifRolle
            {
                ArbeitspreisEurKWh = ARBEITSPREIS,
                Leistungsmodell = DbWerte.LEISTUNGSMODELL_MONATLICH,
                MonatspreisEurKWMonat = monatspreis
            };
        }

        private static StromErloesErgebnis Differenzmethode(StromMatrix m, double monatspreis = 0)
        {
            var eingabe = new StromErloesEingabe
            {
                BedarfMWh = m.BedarfGesamtMWh,
                RestbezugMWh = m.BezugGesamtMWh,
                EinspeisungMWh = m.EinspeisungPvGesamtMWh + m.KwkEinspeisungGesamtMWh,
                LastBedarf = m.LastBedarf,
                LastRestbezug = m.LastBezug
            };
            return StromTarifRechner.Rechne(eingabe, Rolle(monatspreis), Rolle(monatspreis),
                                            null, DE);
        }

        // =====================================================================
        //  Die Bezugsgröße
        // =====================================================================

        /// <summary>
        /// <b>ALT:</b> „Bedarf ohne Anlage" = Bedarf − PV-Eigennutzung = 1.344,2 MWh.
        /// <b>NEU:</b> der Bedarf ohne jede Eigenerzeugung = 1.429,7 MWh; die Differenz ist
        /// die PV-Eigennutzung (85,5 MWh). Der KWK-Eigenanteil bleibt die min-Regel auf den
        /// Bedarf NACH Photovoltaik: 1.094,2 MWh, keine Einspeisung.
        /// </summary>
        [Fact]
        public void Die_Bezugsgroesse_ist_der_Bedarf_ohne_jede_Eigenerzeugung()
        {
            StromMatrix m = Matrix(BEDARF_MWH, PV_MWH, BHKW_MWH, BEZUG_MWH);

            Assert.Equal(BEDARF_MWH, m.BedarfGesamtMWh, 6);            // ALT 1.344,2
            Assert.Equal(PV_MWH, m.PvEigenGesamtMWh, 6);
            Assert.Equal(BHKW_MWH, m.KwkEigenGesamtMWh, 6);            // unverändert
            Assert.Equal(0.0, m.KwkEinspeisungGesamtMWh, 6);
            Assert.Equal(BEZUG_MWH, m.BezugGesamtMWh, 6);

            // Das Lastbild ist das des VOLLEN Bedarfs: 1.429,7 MWh / 8.760 h = 163,2 kW
            // (ALT: 1.344,2 MWh / 8.760 h = 153,4 kW).
            Assert.Equal(BEDARF_MWH * 1000.0 / ZeitreihenSatz.Stunden, m.LastBedarf.MaxJahr, 6);
        }

        /// <summary>
        /// Die min-Regel des KWK-Eigenanteils bleibt auf den Bedarf NACH Photovoltaik:
        /// Bedarf 100, Photovoltaik 30, BHKW 80 kWh je Stunde ergeben 70 Eigen und 10
        /// Einspeisung — der Bedarf ohne jede Eigenerzeugung (100) ändert daran nichts.
        /// </summary>
        [Fact]
        public void Der_KWK_Eigenanteil_bleibt_die_min_Regel_auf_den_Bedarf_nach_PV()
        {
            double je = ZeitreihenSatz.Stunden / 1000.0;           // kWh/h → MWh/a
            StromMatrix m = Matrix(100 * je, 30 * je, 80 * je, 0);

            Assert.Equal(70 * je, m.KwkEigenGesamtMWh, 6);
            Assert.Equal(10 * je, m.KwkEinspeisungGesamtMWh, 6);
            Assert.Equal(100 * je, m.BedarfGesamtMWh, 6);
            Assert.Equal(30 * je, m.PvEigenGesamtMWh, 6);
        }

        /// <summary>Ohne Photovoltaik ist der neue Bedarf der alte: nichts wandert.</summary>
        [Fact]
        public void Ohne_Photovoltaik_bleibt_die_Bezugsgroesse_wie_sie_war()
        {
            StromMatrix m = Matrix(BEDARF_MWH, 0, BHKW_MWH, BEDARF_MWH - BHKW_MWH);

            Assert.Equal(BEDARF_MWH, m.BedarfGesamtMWh, 6);
            Assert.Equal(0.0, m.PvEigenGesamtMWh, 12);
            Assert.Equal(BHKW_MWH, m.KwkEigenGesamtMWh, 6);
        }

        // =====================================================================
        //  Der Anker: 293.245,6 + 22.914,0 = 316.159,6 €/a
        // =====================================================================

        /// <summary>
        /// <b>DER ANKER DER ETAPPE</b> (Mockup Kategorie 7, Rechenweg 07), jetzt über den
        /// Weg des Kerns:
        /// <code>
        /// vermiedene Menge   1.429,7 − 250,0 = 1.179,7 MWh    (ALT 1.344,2 − 250,0 = 1.094,2)
        /// Arbeit             1.179,7 × 288 €/MWh = 339.753,6  (ALT 315.129,6)
        /// § 9b               1.179,7 × 20,00     =  23.594,0  (ALT  21.884,0)
        /// effektiv                                 316.159,6  (ALT 293.245,6)
        /// Blockheizkraftwerk 1.094,2 MWh → 293.245,6 €/a      (ALT: die ganze Menge)
        /// Photovoltaik          85,5 MWh →  22.914,0 €/a      (ALT: keine Zeile)
        /// </code>
        /// </summary>
        [Fact]
        public void Anker_316159_6_ueber_Matrix_Tarifrechner_und_Verteilschluessel()
        {
            StromMatrix m = Matrix(BEDARF_MWH, PV_MWH, BHKW_MWH, BEZUG_MWH);
            StromErloesErgebnis r = Differenzmethode(m);

            Assert.Equal(1179.7, r.VermiedenMengeMWh, 6);
            Assert.Equal(339753.6, r.VermiedenArbeitEur, 2);
            Assert.Equal(0.0, r.VermiedenLeistungEur, 6);

            var erg = new WirtschaftlichkeitErgebnis
            {
                VermiedenMengeMWh = r.VermiedenMengeMWh,
                VermiedenArbeitJahr = r.VermiedenArbeitEur,
                VermiedenGesamtJahr = r.VermiedenGesamtEur,
                // § 9b auf die GANZE Menge — dieselbe Formel wie im Kern
                // (Entlastungssatz × VermiedenMengeMWh).
                VermiedenEntlastung9bJahr = SATZ_9B * r.VermiedenMengeMWh
            };
            Assert.Equal(23594.0, erg.VermiedenEntlastung9bJahr, 2);
            Assert.Equal(316159.6, erg.VermiedenEffektivJahr, 2);

            List<VermiedenAnlageNachweis> zeilen =
                WirtschaftlichkeitCtrl.VermiedenAufteilung(null, m, erg);

            Assert.Equal(2, zeilen.Count);
            VermiedenAnlageNachweis bhkw = zeilen[0];
            VermiedenAnlageNachweis pv = zeilen[1];
            Assert.Equal(WirtZeile.KOMPONENTE_BHKW, bhkw.Komponente);
            Assert.Equal(WirtZeile.KOMPONENTE_PV, pv.Komponente);

            Assert.Equal(BHKW_MWH, bhkw.MengeMWh, 4);
            Assert.Equal(PV_MWH, pv.MengeMWh, 4);
            Assert.Equal(293245.6, bhkw.WirksamEur, 2);
            Assert.Equal(22914.0, pv.WirksamEur, 2);
            Assert.Equal(316159.6, bhkw.WirksamEur + pv.WirksamEur, 2);
            Assert.True(bhkw.IstNaeherung && pv.IstNaeherung);
        }

        /// <summary>
        /// Der Leistungsanteil der Bezugsseite hängt am Lastbild des VOLLEN Bedarfs:
        /// Bei 10 €/(kW·Monat) kostet der Bezug ohne Anlage 12 × 163,2 kW × 10 € =
        /// 19.585 €/a (ALT mit 153,4 kW: 18.414 €/a); der Reststrom bleibt 12 × 28,5 kW
        /// × 10 € = 3.425 €/a.
        /// </summary>
        [Fact]
        public void Der_Leistungsanteil_haengt_am_Lastbild_des_vollen_Bedarfs()
        {
            StromMatrix m = Matrix(BEDARF_MWH, PV_MWH, BHKW_MWH, BEZUG_MWH);
            StromErloesErgebnis r = Differenzmethode(m, monatspreis: 10.0);

            double spitzeBedarf = BEDARF_MWH * 1000.0 / ZeitreihenSatz.Stunden;
            double spitzeBezug = BEZUG_MWH * 1000.0 / ZeitreihenSatz.Stunden;
            Assert.Equal(12 * spitzeBedarf * 10.0, r.Bezug.LeistungEur, 6);
            Assert.Equal(12 * spitzeBezug * 10.0, r.Reststrom.LeistungEur, 6);
            Assert.Equal(12 * (spitzeBedarf - spitzeBezug) * 10.0, r.VermiedenLeistungEur, 6);
        }

        /// <summary>
        /// Ohne Photovoltaik trägt der Schlüssel allein das Blockheizkraftwerk — eine
        /// Anlage, exakt, ohne Näherungsvermerk; mit Photovoltaik ohne BHKW trägt er allein
        /// die Photovoltaik.
        /// </summary>
        [Fact]
        public void Eine_Anlage_bekommt_alles()
        {
            StromMatrix nurBhkw = Matrix(BEDARF_MWH, 0, BHKW_MWH, BEDARF_MWH - BHKW_MWH);
            List<VermiedenAnlageNachweis> a = WirtschaftlichkeitCtrl.VermiedenAufteilung(
                null, nurBhkw, Ergebnis(nurBhkw));
            Assert.Single(a);
            Assert.Equal(WirtZeile.KOMPONENTE_BHKW, a[0].Komponente);
            Assert.False(a[0].IstNaeherung);

            StromMatrix nurPv = Matrix(BEDARF_MWH, PV_MWH, 0, BEDARF_MWH - PV_MWH);
            List<VermiedenAnlageNachweis> b = WirtschaftlichkeitCtrl.VermiedenAufteilung(
                null, nurPv, Ergebnis(nurPv));
            Assert.Single(b);
            Assert.Equal(WirtZeile.KOMPONENTE_PV, b[0].Komponente);
            Assert.Equal(PV_MWH, b[0].MengeMWh, 6);
            Assert.False(b[0].IstNaeherung);
        }

        private static WirtschaftlichkeitErgebnis Ergebnis(StromMatrix m)
        {
            StromErloesErgebnis r = Differenzmethode(m);
            return new WirtschaftlichkeitErgebnis
            {
                VermiedenMengeMWh = r.VermiedenMengeMWh,
                VermiedenArbeitJahr = r.VermiedenArbeitEur,
                VermiedenGesamtJahr = r.VermiedenGesamtEur,
                VermiedenEntlastung9bJahr = SATZ_9B * r.VermiedenMengeMWh
            };
        }
    }
}
