using System.Collections.Generic;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ETAPPE B7 — die Rubrik „Erlöse und Vorteile" (Konzept § 2.6).
    ///
    /// <para><b>Warum es diese Prüfstände gibt.</b> Bis B7 gab es keinen einzigen
    /// Kernfall, der die Erlösseite als ZUSAMMENHANG gemessen hätte: Es gab Anzeigefälle
    /// mit festen Zahlen und Rechenfälle je Vorschrift, aber nichts, was geprüft hätte,
    /// was zusammen in eine Summe darf und was nicht. Genau daran hängt die Rubrik —
    /// Block B in der Summe wäre eine Doppelzählung, und sie fiele niemandem auf, weil
    /// die Zahl plausibel aussähe.</para>
    ///
    /// <para><b>Die Zahlen sind hergeleitet, nicht abgeschrieben:</b> Ein BHKW mit
    /// 7.316 €/a KWK-Zuschlag, 5.119 €/a Energiesteuer-Entlastung, 906 €/a
    /// § 9b-Entlastung, 1.234 €/a Einspeiseerlös; ausgewiesen 86.000 €/a Befreiung nach
    /// § 9 Abs. 1 Nr. 3 und 4.321 €/a vermiedene Kosten bei 20 MWh vermiedener Menge.
    /// Bei 20,00 €/MWh sind das 400 €/a entgangene Entlastung — effektiv 3.921 €/a.</para>
    /// </summary>
    public class ErloesrubrikTests
    {
        /// <summary>Die Fälle halten deutsche Ressourcentexte gegen <c>Contains</c> —
        /// ohne Pinnung wären sie auf dem Windows-Läufer (en-US) rot.</summary>
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        private const double SATZ_9B_EUR_MWH = 20.00;   // § 9b StromStG, Katalogsatz 2026

        private static WirtschaftlichkeitErgebnis Lauf(bool produzierendesGewerbe = true)
        {
            var e = new WirtschaftlichkeitErgebnis
            {
                IdProjekt = 1,
                IstStamm = false,
                Anzeige = "Variante",
                Szenario = WirtschaftlichkeitSzenario.ERWARTET,
                KwkgErloesJahr1 = 7316,
                KwkgVbhElektrisch = 4200,
                EnergiesteuerJahr1 = 5119,
                StromsteuerEntlastungJahr1 = 906,
                StromsteuerBefreiungJahr1 = 86000,
                StromsteuerBefreiungAlsErloes = false,      // Vorgabe seit B6: AUSWEIS
                EinspeiseerloesJahr = 1234,
                EinspeiseerloesKwkJahr = 1234,
                VermiedenArbeitJahr = 4662,
                VermiedenLeistungJahr = -341,
                VermiedenGesamtJahr = 4321,
                VermiedenMengeMWh = 20,
                Kapitalwert = 123456
            };
            if (produzierendesGewerbe)
            {
                e.ProduzierendesGewerbe = true;
                e.VermiedenEntlastung9bJahr = SATZ_9B_EUR_MWH * e.VermiedenMengeMWh;   // 400 €/a
            }
            return e;
        }

        private static List<WirtZeile> Rubrik(WirtschaftlichkeitErgebnis e)
        {
            var menge = new List<WirtschaftlichkeitErgebnis> { e };
            return WirtschaftlichkeitZeilen.Sichtbare(
                WirtschaftlichkeitZeilen.Kennzahlen(menge, null), menge);
        }

        private static WirtZeile Zeile(List<WirtZeile> zeilen, string schluessel)
        {
            foreach (WirtZeile z in zeilen) if (z.Schluessel == schluessel) return z;
            return null;
        }

        // =================================================================
        //  Block A: was summiert wird
        // =================================================================

        /// <summary>
        /// Die Summenzeile des Blocks A ist die Summe GENAU der A-Zeilen, die über ihr
        /// stehen — nicht eine zweite, von Hand gepflegte Rechnung.
        /// </summary>
        [Fact]
        public void Die_Summe_des_Blocks_A_ist_die_Summe_seiner_Zeilen()
        {
            WirtschaftlichkeitErgebnis e = Lauf();
            List<WirtZeile> zeilen = Rubrik(e);

            double erwartet = 0;
            foreach (WirtZeile z in zeilen)
            {
                if (z.Block != WirtZeile.BLOCK_A) continue;
                if (z.IstUeberschrift || z.IstSumme || z.Einzug > 0 || z.IstText) continue;
                double? w = z.Wert == null ? null : z.Wert(e);
                if (w.HasValue) erwartet += w.Value;
            }

            WirtZeile summe = Zeile(zeilen, "ERL_A_SUMME");
            Assert.NotNull(summe);
            Assert.Equal(erwartet, summe.Wert(e).Value, 6);

            // Und das ist die hergeleitete Zahl: 7.316 + 5.119 + 906 + 1.234.
            Assert.Equal(14575.0, summe.Wert(e).Value, 6);
        }

        /// <summary>
        /// DIE GEGENPROBE zur Summenzeile: Nähme man die Block-B-Zeile der vermiedenen
        /// Kosten hinzu, käme 18.896 € statt 14.575 € heraus — eine Zahl, die zu einem
        /// Kapitalwert gehörte, den es nicht gibt. Der Prüfstand hält beide Zahlen
        /// auseinander, damit ein späterer „Fix", der Block B einrechnet, hier rot wird.
        /// </summary>
        [Fact]
        public void Gegenprobe_Block_B_in_der_Summe_waere_eine_andere_Zahl()
        {
            WirtschaftlichkeitErgebnis e = Lauf();
            WirtZeile summe = Zeile(Rubrik(e), "ERL_A_SUMME");

            double mitAusweis = summe.Wert(e).Value + e.VermiedenGesamtJahr;
            Assert.Equal(18896.0, mitAusweis, 6);
            Assert.NotEqual(mitAusweis, summe.Wert(e).Value);
        }

        /// <summary>Keine Zeile des Ausweisblocks steht in der Summandenliste — geprüft
        /// über die Blockkennung, die der einzige Weg in die Summe ist.</summary>
        [Fact]
        public void Keine_Zeile_des_Blocks_B_traegt_die_Kennung_des_Blocks_A()
        {
            foreach (WirtZeile z in Rubrik(Lauf()))
                if (z.Schluessel.StartsWith("ERL_B") || z.Schluessel.StartsWith("VERMIEDEN") ||
                    z.Schluessel == "PV_VERMIEDEN" || z.Schluessel == "PV_KAPPUNG")
                    Assert.NotEqual(WirtZeile.BLOCK_A, z.Block);
        }

        // =================================================================
        //  B1: die § 9b-Korrektur des Ausweises (Konzept § 2.6, Klarstellung 1)
        // =================================================================

        /// <summary>
        /// effektiv = brutto − 20,00 €/MWh × vermiedene Menge. Das ist die eine
        /// Ergebniswirkung der Etappe — und sie trifft den AUSWEIS, nicht den
        /// Kapitalwert.
        /// </summary>
        [Fact]
        public void Vermiedene_Kosten_effektiv_sind_brutto_abzueglich_der_Paragraf_9b_Entlastung()
        {
            WirtschaftlichkeitErgebnis e = Lauf();
            List<WirtZeile> zeilen = Rubrik(e);

            Assert.Equal(400.0, e.VermiedenEntlastung9bJahr, 6);      // 20 MWh × 20,00 €
            Assert.Equal(3921.0, e.VermiedenEffektivJahr, 6);         // 4.321 − 400

            WirtZeile abzug = Zeile(zeilen, "ERL_B1_ABZUG_9B");
            WirtZeile effektiv = Zeile(zeilen, "ERL_B1_EFFEKTIV");
            Assert.NotNull(abzug);
            Assert.NotNull(effektiv);
            Assert.Equal(-400.0, abzug.Wert(e).Value, 6);             // als Abzug dargestellt
            Assert.Equal(3921.0, effektiv.Wert(e).Value, 6);

            // Der Kapitalwert bleibt, was er war: Die Korrektur hängt an keiner Reihe.
            Assert.Equal(123456.0, e.Kapitalwert.Value, 6);
        }

        /// <summary>
        /// Ohne produzierendes Gewerbe gibt es keine Entlastung — dann IST brutto
        /// effektiv, und die beiden Korrekturzeilen entfallen. Zwei gleiche Zahlen
        /// untereinander erklärten nichts.
        /// </summary>
        [Fact]
        public void Ohne_produzierendes_Gewerbe_entfaellt_die_Korrektur_und_brutto_ist_effektiv()
        {
            WirtschaftlichkeitErgebnis e = Lauf(produzierendesGewerbe: false);
            List<WirtZeile> zeilen = Rubrik(e);

            Assert.Equal(0.0, e.VermiedenEntlastung9bJahr, 6);
            Assert.Equal(e.VermiedenGesamtJahr, e.VermiedenEffektivJahr, 6);
            Assert.Null(Zeile(zeilen, "ERL_B1_ABZUG_9B"));
            Assert.Null(Zeile(zeilen, "ERL_B1_EFFEKTIV"));

            // Die A6-Zeile steht trotzdem da — mit 0 und dem Grund dafür.
            WirtZeile a6 = Zeile(zeilen, "ERL_A_STROMST_ENTLASTUNG");
            Assert.NotNull(a6);
        }

        // =================================================================
        //  A7 folgt dem Modus des § 9 Abs. 1 Nr. 3 (Etappe B6)
        // =================================================================

        /// <summary>
        /// Modus AUSWEIS (Vorgabe): die Befreiung steht in Block B und NICHT in der
        /// Summe. Modus ERLOES: sie steht in Block A und hebt die Summe um ihren Betrag.
        /// </summary>
        [Fact]
        public void A7_folgt_dem_Modus_des_Paragrafen_9_Abs_1_Nr_3()
        {
            WirtschaftlichkeitErgebnis ausweis = Lauf();
            List<WirtZeile> zA = Rubrik(ausweis);
            Assert.Null(Zeile(zA, "ERL_A_STROMST_BEFREIUNG"));
            Assert.NotNull(Zeile(zA, "ERL_B_STROMST_BEFREIUNG"));
            Assert.Equal(14575.0, Zeile(zA, "ERL_A_SUMME").Wert(ausweis).Value, 6);

            WirtschaftlichkeitErgebnis erloes = Lauf();
            erloes.StromsteuerBefreiungAlsErloes = true;
            List<WirtZeile> zE = Rubrik(erloes);
            Assert.NotNull(Zeile(zE, "ERL_A_STROMST_BEFREIUNG"));
            Assert.Null(Zeile(zE, "ERL_B_STROMST_BEFREIUNG"));
            Assert.Equal(14575.0 + 86000.0, Zeile(zE, "ERL_A_SUMME").Wert(erloes).Value, 6);
        }

        // =================================================================
        //  Die Nullzeile mit Klartext (Anwenderbefund 17.09.2026)
        // =================================================================

        /// <summary>
        /// Eine A-Zeile erscheint, sobald das Projekt eine Anlage führt, für die die
        /// Position gilt — auch bei Betrag 0. Und sie sagt dann, WARUM sie 0 ist; genau
        /// das fehlte dem Anwender, der die Vergütungen „nicht dargestellt" fand.
        /// </summary>
        [Fact]
        public void Eine_Nullzeile_nennt_die_fehlende_Grundlage_im_Klartext()
        {
            var e = new WirtschaftlichkeitErgebnis
            {
                IdProjekt = 1,
                Anzeige = "Variante",
                KwkgVbhElektrisch = 4200,      // ein BHKW läuft …
                KwkgErloesJahr1 = 0            // … aber kein Satz ist gepflegt
            };
            List<WirtZeile> zeilen = Rubrik(e);

            WirtZeile kwkg = Zeile(zeilen, "ERL_A_KWKG");
            Assert.NotNull(kwkg);

            string anzeige = kwkg.Anzeige(e, CultureInfo.GetCultureInfo("de-DE"));
            Assert.StartsWith("0", anzeige);
            Assert.Contains("—", anzeige);
            Assert.Contains("KWK-Zuschlagssatz", anzeige);

            // Excel bekommt trotzdem die blanke Zahl — sonst wären Filter und
            // Diagramme des Blattes hinüber.
            Assert.Equal(0.0, kwkg.ExcelWert(e).Value, 6);
        }

        /// <summary>
        /// Ohne jede Anlage bleibt die Rubrik schlank: Ein Projekt ohne BHKW bekommt
        /// keine KWKG- und keine Energiesteuerzeile. „Immer zeigen" heißt nicht
        /// „überall zeigen".
        /// </summary>
        [Fact]
        public void Ohne_BHKW_stehen_KWKG_und_Energiesteuer_nicht_in_der_Rubrik()
        {
            var e = new WirtschaftlichkeitErgebnis { IdProjekt = 1, Anzeige = "Variante" };
            List<WirtZeile> zeilen = Rubrik(e);

            Assert.Null(Zeile(zeilen, "ERL_A_KWKG"));
            Assert.Null(Zeile(zeilen, "ERL_A_ENERGIESTEUER"));
            // § 9b und der Einspeiseerlös hängen an keiner Anlage und bleiben.
            Assert.NotNull(Zeile(zeilen, "ERL_A_STROMST_ENTLASTUNG"));
            Assert.NotNull(Zeile(zeilen, "EINSPEISEERLOES"));
        }

        // =================================================================
        //  Eine Sichtbarkeitsregel für alle drei Ausgaben
        // =================================================================

        /// <summary>
        /// Eine Blocküberschrift ohne Zeilen und eine Summe ohne Summanden sind
        /// Behauptungen — beide fallen weg. Geprüft am leeren Ergebnis, das nur eine
        /// Fehlermeldung trägt.
        /// </summary>
        [Fact]
        public void Ein_Ausweisblock_ohne_Zeilen_verschwindet_samt_Ueberschrift()
        {
            var e = new WirtschaftlichkeitErgebnis { IdProjekt = 1, Anzeige = "Variante" };
            List<WirtZeile> zeilen = Rubrik(e);

            Assert.Null(Zeile(zeilen, "ERL_KOPF_B"));
            // Block A bleibt: § 9b und Einspeiseerlös gelten für jedes Projekt.
            Assert.NotNull(Zeile(zeilen, "ERL_KOPF_A"));
            Assert.NotNull(Zeile(zeilen, "ERL_A_SUMME"));
        }

        /// <summary>
        /// Die anlagenscharfe Aufschlüsselung der Energiekosten summiert sich auf die
        /// Zeile darüber — sie erklärt die Zahl, sie ändert sie nicht.
        /// </summary>
        [Fact]
        public void Die_Aufschluesselung_der_Energiekosten_summiert_sich_auf_die_Gesamtzeile()
        {
            var e = new WirtschaftlichkeitErgebnis
            {
                IdProjekt = 1,
                Anzeige = "Variante",
                EnergiekostenJahr = 9000,
                EnergiekostenJeAnlage = new List<EnergieAnlageNachweis>
                {
                    new EnergieAnlageNachweis { Anlage = "BHKW 1", Traeger = "Erdgas",
                        MengeMWh = 100, MengeAbrechnung = 100000, Einheit = "kWh",
                        PreisJeEinheit = 0.06, KostenEur = 6000 },
                    new EnergieAnlageNachweis { Anlage = "Kessel 1", Traeger = "Erdgas",
                        MengeMWh = 50, MengeAbrechnung = 50000, Einheit = "kWh",
                        PreisJeEinheit = 0.06, KostenEur = 3000 }
                }
            };
            List<WirtZeile> zeilen = Rubrik(e);

            double summe = 0;
            int gefunden = 0;
            foreach (WirtZeile z in zeilen)
                if (z.Schluessel.StartsWith("ENERGIEKOSTEN_ANLAGE_"))
                { summe += z.Wert(e).Value; gefunden++; }

            Assert.Equal(2, gefunden);
            Assert.Equal(9000.0, summe, 6);
            Assert.Equal(e.EnergiekostenJahr.Value, summe, 6);

            string herleitung = WirtschaftlichkeitZeilen.AnlageHerleitung(
                e, "BHKW 1", CultureInfo.GetCultureInfo("de-DE"));
            Assert.Contains("kWh", herleitung);
            Assert.Contains("Erdgas", herleitung);
        }
    }
}
