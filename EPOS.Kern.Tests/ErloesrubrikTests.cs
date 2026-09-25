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
    public class ErloesrubrikTests : System.IDisposable
    {
        /// <summary>Die Fälle halten deutsche Ressourcentexte gegen <c>Contains</c> —
        /// ohne Pinnung wären sie auf dem Windows-Läufer (en-US) rot.</summary>
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

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
                // AUFTRAG U7 — ein frisch gerechneter Lauf kennt die Aufteilung
                // immer; 4.119 € nach § 53a am BHKW, 1.000 € nach § 54 am Kessel.
                // Die SUMME bleibt 5.119 €, und damit bleibt die Summe des Blocks A,
                // was sie vor U7 war.
                EnergiesteuerAufgeteilt = true,
                Energiesteuer53Jahr1 = 4119,
                Energiesteuer54Jahr1 = 1000,
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

            // ETAPPE E5 (Entscheid Q16): Die Zelle zeigt „— ‹Grund›" statt „0 — ‹Grund›" —
            // eine 0 steht nur, wo null gerechnet wurde; hier fehlt die Grundlage.
            string anzeige = kwkg.Anzeige(e, CultureInfo.GetCultureInfo("de-DE"));
            Assert.StartsWith("— ", anzeige);
            Assert.DoesNotContain("0 —", anzeige);
            Assert.Contains("KWK-Zuschlagssatz", anzeige);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.WIRT_GRUND_KWKG, kwkg.Grund(e));

            // Excel bleibt numerisch: keine 0 als Wert und kein Text — die Zelle bleibt
            // leer, damit Filter und Diagramme des Blattes keine Null behaupten.
            Assert.Null(kwkg.ExcelWert(e));
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

        // =================================================================
        //  AUFTRAG U7 — die Energiesteuer in zwei Zeilen (Befund B7-1)
        // =================================================================

        /// <summary>
        /// § 53/§ 53a und § 54 stehen in ZWEI Zeilen, und ihre Summe ist die eine
        /// Zahl, die bis U7 allein dastand. Die Summe des Blocks A ändert sich
        /// dadurch nicht — die Etappe hat keine Rechenwirkung.
        /// </summary>
        [Fact]
        public void Die_Energiesteuer_steht_in_zwei_Zeilen_und_die_Summe_bleibt()
        {
            WirtschaftlichkeitErgebnis e = Lauf();
            List<WirtZeile> zeilen = Rubrik(e);

            WirtZeile a53 = Zeile(zeilen, "ERL_A_ENERGIESTEUER");
            WirtZeile a54 = Zeile(zeilen, "ERL_A_ENERGIESTEUER_54");
            Assert.NotNull(a53);
            Assert.NotNull(a54);

            Assert.Equal(4119.0, a53.Wert(e).Value, 6);
            Assert.Equal(1000.0, a54.Wert(e).Value, 6);
            Assert.Equal(e.EnergiesteuerJahr1, a53.Wert(e).Value + a54.Wert(e).Value, 6);

            // Beide Zeilen sind Summanden des Blocks A; die Summe ist die von vor U7.
            Assert.Equal(WirtZeile.BLOCK_A, a53.Block);
            Assert.Equal(WirtZeile.BLOCK_A, a54.Block);
            Assert.Equal(14575.0, Zeile(zeilen, "ERL_A_SUMME").Wert(e).Value, 6);

            // Zwei Zeilen, zwei Rechtsgrundlagen — und der Titel nennt sie.
            Assert.Contains("53", a53.Titel);
            Assert.Contains("54", a54.Titel);
        }

        /// <summary>
        /// DER RÜCKFALL: Ein vor U7 gebuchter Stand kennt seine Aufteilung nicht
        /// (Nachweisumschlag der Fassung ≤ 3). Dann bleibt es bei der EINEN Zeile
        /// über beide Vorschriften, die den ganzen Betrag trägt — zwei Nullzeilen
        /// neben einer Ergebnisspalte mit Betrag wären eine Behauptung, und die
        /// Summe des Blocks A fiele um genau diesen Betrag zu klein aus.
        /// </summary>
        [Fact]
        public void Ohne_bekannte_Aufteilung_bleibt_es_bei_einer_Zeile()
        {
            WirtschaftlichkeitErgebnis e = Lauf();
            e.EnergiesteuerAufgeteilt = false;
            e.Energiesteuer53Jahr1 = 0;
            e.Energiesteuer54Jahr1 = 0;

            List<WirtZeile> zeilen = Rubrik(e);

            WirtZeile eine = Zeile(zeilen, "ERL_A_ENERGIESTEUER");
            Assert.NotNull(eine);
            Assert.Null(Zeile(zeilen, "ERL_A_ENERGIESTEUER_54"));

            Assert.Equal(5119.0, eine.Wert(e).Value, 6);
            Assert.Equal(14575.0, Zeile(zeilen, "ERL_A_SUMME").Wert(e).Value, 6);
        }

        /// <summary>
        /// Ein einziger Stand OHNE Aufteilung zieht die ganze Gruppe auf die
        /// Gesamtzeile zurück: Word, Excel und Reiter bauen EINE Tabelle über alle
        /// Spalten, und die darf nicht für die eine Spalte zwei Zeilen zeigen und
        /// für die andere eine.
        /// </summary>
        [Fact]
        public void Ein_Stand_ohne_Aufteilung_zieht_die_ganze_Gruppe_zurueck()
        {
            WirtschaftlichkeitErgebnis frisch = Lauf();
            WirtschaftlichkeitErgebnis gebucht = Lauf();
            gebucht.IdProjekt = 2;
            gebucht.EnergiesteuerAufgeteilt = false;
            gebucht.Energiesteuer53Jahr1 = 0;
            gebucht.Energiesteuer54Jahr1 = 0;

            var menge = new List<WirtschaftlichkeitErgebnis> { frisch, gebucht };
            List<WirtZeile> zeilen = WirtschaftlichkeitZeilen.Sichtbare(
                WirtschaftlichkeitZeilen.Kennzahlen(menge, null), menge);

            Assert.Null(Zeile(zeilen, "ERL_A_ENERGIESTEUER_54"));
            WirtZeile eine = Zeile(zeilen, "ERL_A_ENERGIESTEUER");
            Assert.NotNull(eine);
            Assert.Equal(5119.0, eine.Wert(frisch).Value, 6);
            Assert.Equal(5119.0, eine.Wert(gebucht).Value, 6);
        }

        /// <summary>
        /// Unter jeder der beiden Geldzeilen steht ihre HERLEITUNG aus dem Nachweis
        /// des Laufs: Menge in der gesetzlichen Einheit, Satz, Betrag — und beim
        /// § 54 der abgezogene Sockelbetrag. Die Zahlen des Beispielprojekts
        /// (Rechenweg 05 und 07): 4.796,99 MWh × 4,42 €/MWh = 21.202,71 €/a und
        /// 2.272,26 MWh × 1,38 €/MWh = 3.135,72 €/a abzüglich 250 €/a.
        /// </summary>
        [Fact]
        public void Unter_jeder_Steuerzeile_steht_ihre_Herleitung()
        {
            WirtschaftlichkeitErgebnis e = Beispielprojekt();
            List<WirtZeile> zeilen = Rubrik(e);

            WirtZeile h53 = Zeile(zeilen, "ERL_A_ENERGIESTEUER_SATZ");
            WirtZeile h54 = Zeile(zeilen, "ERL_A_ENERGIESTEUER_54_SATZ");
            Assert.NotNull(h53);
            Assert.NotNull(h54);
            Assert.Equal(1, h53.Einzug);
            Assert.Equal(1, h54.Einzug);

            // Die Mengen stehen mit einer Nachkommastelle — wie im Rechenweg und im
            // Mockup („4.797,2 MWh (H_s)"); die Beträge mit zweien.
            string t53 = h53.Text(e);
            Assert.Contains("4.797,0", t53);
            Assert.Contains("MWh", t53);
            Assert.Contains("4,42", t53);
            Assert.Contains("21.202,71", t53);

            string t54 = h54.Text(e);
            Assert.Contains("2.272,3", t54);
            Assert.Contains("1,38", t54);
            Assert.Contains("Sockelbetrag", t54);
            Assert.Contains("250", t54);

            // Eine Textzeile bekommt in Excel keine Zahl — sonst stünde Text in
            // einer Wertspalte.
            Assert.Null(h53.ExcelWert(e));
            Assert.Null(h54.ExcelWert(e));
        }

        /// <summary>
        /// <b>Die Zahlenprobe der Etappe am Beispielprojekt der Kategorie 7:</b>
        /// Die Energiesteuerzeile zerfällt in § 53a = 21.202,71 €/a beim
        /// Blockheizkraftwerk und § 54 = 2.885,72 €/a beim Kessel; zusammen sind es
        /// die 24.088,43 €/a, die bis U7 als eine Zahl dastanden. Die Sollwerte
        /// rechnet <c>SteuerGutschriftRechnerTests</c> aus Mengen und Katalogsätzen
        /// nach — hier wird geprüft, dass die RUBRIK sie unverändert weiterreicht.
        /// </summary>
        [Fact]
        public void Zahlenprobe_Kategorie_7_die_Rubrik_zeigt_beide_Betraege()
        {
            WirtschaftlichkeitErgebnis e = Beispielprojekt();
            List<WirtZeile> zeilen = Rubrik(e);

            Assert.Equal(21202.71, Zeile(zeilen, "ERL_A_ENERGIESTEUER").Wert(e).Value, 2);
            Assert.Equal(2885.72, Zeile(zeilen, "ERL_A_ENERGIESTEUER_54").Wert(e).Value, 2);
            Assert.Equal(24088.43, e.EnergiesteuerJahr1, 2);
        }

        // =================================================================
        //  AUFTRAG 9d — der Grund je Position (Befund B7-4)
        // =================================================================

        /// <summary>
        /// Eine Nullzeile nennt, was der RECHENWEG festgestellt hat — nicht nur die
        /// Bedingung der Position. Bis 9d stand dieselbe Auskunft ausschließlich im
        /// Hinweisfeld des Laufs, als ein mit „ | " verbundener Text über alle
        /// Vorschriften; einer einzelnen Zeile war sie nicht zuzuordnen.
        /// </summary>
        [Fact]
        public void Eine_Nullzeile_nennt_den_Grund_des_Laufs_in_der_Herleitungszeile()
        {
            WirtschaftlichkeitErgebnis e = Beispielprojekt();
            e.Energiesteuer54Jahr1 = 0;
            e.Energiesteuer54SockelJahr1 = 0;
            e.EnergiesteuerNachweise.RemoveAll(n => n.Ist54);
            e.EnergiesteuerJahr1 = e.Energiesteuer53Jahr1;
            e.PositionsGruende[SteuerPosition.ENERGIEST_54] =
                "§ 54 EnergieStG: kein Unternehmen des produzierenden Gewerbes";

            List<WirtZeile> zeilen = Rubrik(e);

            WirtZeile grund = Zeile(zeilen, "ERL_A_ENERGIESTEUER_54_SATZ");
            Assert.NotNull(grund);
            Assert.Contains("produzierenden Gewerbes", grund.Text(e));

            // Die Bedingung steht weiterhin an der Geldzeile selbst (B7) — Diagnose
            // und Bedingung sind zwei verschiedene Auskünfte. ETAPPE E5 (Q16): als
            // „— ‹Grund›", nicht mehr als „0 — ‹Grund›".
            string zelle = Zeile(zeilen, "ERL_A_ENERGIESTEUER_54")
                           .Anzeige(e, CultureInfo.GetCultureInfo("de-DE"));
            Assert.StartsWith("— ", zelle);
            Assert.Contains("produzierendes Gewerbe", zelle);
        }

        /// <summary>
        /// Ohne Feststellung des Laufs bleibt die Herleitungszeile LEER und entfällt.
        /// Ein erfundener Grund sähe aus wie eine Feststellung — das ist genau der
        /// Fehler, den 9d abstellt, und nicht einer, den es einführen darf.
        /// </summary>
        [Fact]
        public void Ohne_Feststellung_bleibt_die_Herleitungszeile_weg()
        {
            WirtschaftlichkeitErgebnis e = Lauf();
            e.KwkgModule = new List<KwkgModulNachweis>();     // kein Modulnachweis
            e.KwkgErloesJahr1 = 0;

            List<WirtZeile> zeilen = Rubrik(e);

            Assert.NotNull(Zeile(zeilen, "ERL_A_KWKG"));
            Assert.Null(Zeile(zeilen, "ERL_A_KWKG_GRUND"));
        }

        /// <summary>
        /// Der KWKG-Grund kommt aus dem MODULNACHWEIS — aus den Größen, mit denen der
        /// KWKG-Rechner gerechnet hat. Drei Befunde, drei Sätze: keine
        /// zuschlagsfähige Menge, kein gepflegter Satz, erschöpftes Kontingent.
        /// </summary>
        [Theory]
        [InlineData(0.0, 5.0, 0, "KWK-Erzeugung")]
        [InlineData(100.0, 0.0, 0, "Zuschlagssatz")]
        [InlineData(100.0, 5.0, 1, "Kontingent")]
        public void Der_KWKG_Grund_kommt_aus_dem_Modulnachweis(
            double mengeMWh, double satzCt, int erschoepftAbJahr, string erwartet)
        {
            WirtschaftlichkeitErgebnis e = Lauf();
            e.KwkgErloesJahr1 = 0;
            e.KwkgModule = new List<KwkgModulNachweis>
            {
                new KwkgModulNachweis
                {
                    Bezeichner = "BHKW 1",
                    EigenMWh = mengeMWh,
                    SatzEigenCt = satzCt,
                    ErschoepftAbJahr = erschoepftAbJahr
                }
            };

            WirtZeile grund = Zeile(Rubrik(e), "ERL_A_KWKG_GRUND");
            Assert.NotNull(grund);
            Assert.Contains(erwartet, grund.Text(e));
        }

        /// <summary>
        /// „Gar keine Einspeisung" und „eingespeist, aber keine Vergütung gepflegt"
        /// sind zwei verschiedene Befunde; der Modulnachweis unterscheidet sie, weil
        /// er die eingespeiste MENGE führt.
        /// </summary>
        [Theory]
        [InlineData(0.0, "keine Einspeisung")]
        [InlineData(495.0, "keine Vergütung")]
        public void Der_Einspeisegrund_unterscheidet_Menge_und_Verguetung(
            double einspeisungMWh, string erwartet)
        {
            WirtschaftlichkeitErgebnis e = Lauf();
            e.EinspeiseerloesJahr = 0;
            e.EinspeiseerloesKwkJahr = 0;
            e.KwkgModule = new List<KwkgModulNachweis>
            {
                new KwkgModulNachweis { Bezeichner = "BHKW 1", EinspeisungMWh = einspeisungMWh }
            };

            WirtZeile grund = Zeile(Rubrik(e), "EINSPEISEERLOES_GRUND");
            Assert.NotNull(grund);
            Assert.Contains(erwartet, grund.Text(e));
        }

        /// <summary>
        /// Der Grund erreicht ALLE drei Ausgaben, weil er eine TEXTzeile des einen
        /// Zeilenkatalogs ist: Seite und Wortbericht schreiben
        /// <c>WirtZeile.Anzeige</c>, Excel den Text der Zeile. Der Zelltext einer
        /// WERTspalte käme dort nie an — die bleibt numerisch.
        /// </summary>
        [Fact]
        public void Der_Grund_erreicht_auch_Excel_weil_er_eine_Textzeile_ist()
        {
            WirtschaftlichkeitErgebnis e = Lauf();
            e.StromsteuerEntlastungJahr1 = 0;
            e.PositionsGruende[SteuerPosition.STROMST_ENTLASTUNG] =
                "§ 9b StromStG: Sockelbetrag 250 €/a nicht erreicht";

            WirtZeile grund = Zeile(Rubrik(e), "ERL_A_STROMST_ENTLASTUNG_GRUND");
            Assert.NotNull(grund);
            Assert.True(grund.IstText);
            Assert.Contains("Sockelbetrag", grund.Text(e));
            Assert.Contains("Sockelbetrag",
                            grund.Anzeige(e, CultureInfo.GetCultureInfo("de-DE")));
            Assert.Null(grund.ExcelWert(e));       // keine Zahl in der Wertspalte
        }

        // =================================================================
        //  AUFTRAG U6 — die Komponente gliedert innen (Konzept § 2.6, Q15/A12)
        // =================================================================

        /// <summary>
        /// <b>DIE ZAHLENPROBE DER ETAPPE</b> (Mockup Kategorie 7, Abschnitt „Erlösrubrik"):
        /// Die § 9b-Kette ergibt 339.753,6 − 23.594,0 = 316.159,6 €/a, und sie teilt
        /// sich nach den Eigenverbrauchsmengen je Anlage auf
        /// <b>293.245,6</b> (Blockheizkraftwerk, 1.179,7 − 85,5 MWh) und
        /// <b>22.914,0</b> (Photovoltaik, 85,5 MWh).
        ///
        /// <para>Gemessen wird der VERTEILSCHLÜSSEL selbst — er ist die eine Stelle, an
        /// der die Aufteilung entsteht. Die Gegenprobe steht gleich darunter: Die Summe
        /// der Anteile ist bitgenau die projektweite Größe; verteilt wird, nicht
        /// gerechnet.</para>
        /// </summary>
        [Fact]
        public void Zahlenprobe_U6_die_vermiedenen_Kosten_teilen_sich_nach_Eigenverbrauch()
        {
            const double MENGE = 1179.7;                 // vermiedener Bezug [MWh/a]
            const double PV_MWH = 85.5;                  // Eigenverbrauch Photovoltaik
            const double BHKW_MWH = MENGE - PV_MWH;      // 1.094,2 — der Rest
            const double ARBEIT = MENGE * 1000.0 * 0.2880;        // 28,80 ct/kWh
            const double ENTLASTUNG = MENGE * 20.00;              // § 9b, 20,00 €/MWh

            List<VermiedenAnlageNachweis> zeilen = VermiedenAnlageNachweis.Verteile(
                new List<VermiedenAnlageNachweis>
                {
                    new VermiedenAnlageNachweis
                    { Komponente = WirtZeile.KOMPONENTE_BHKW, EigenMWh = BHKW_MWH },
                    new VermiedenAnlageNachweis
                    { Komponente = WirtZeile.KOMPONENTE_PV, EigenMWh = PV_MWH }
                },
                MENGE, ARBEIT, ENTLASTUNG);

            Assert.Equal(2, zeilen.Count);
            VermiedenAnlageNachweis bhkw = zeilen[0];
            VermiedenAnlageNachweis pv = zeilen[1];

            Assert.Equal(BHKW_MWH, bhkw.MengeMWh, 4);
            Assert.Equal(PV_MWH, pv.MengeMWh, 4);

            // Blockheizkraftwerk: 1.094,2 MWh × 28,80 ct − 1.094,2 MWh × 20,00 €/MWh
            Assert.Equal(315129.6, bhkw.ArbeitEur, 2);
            Assert.Equal(21884.0, bhkw.Entlastung9bEur, 2);
            Assert.Equal(293245.6, bhkw.WirksamEur, 2);

            // Photovoltaik: 85,5 MWh × 28,80 ct − 85,5 MWh × 20,00 €/MWh
            Assert.Equal(24624.0, pv.ArbeitEur, 2);
            Assert.Equal(1710.0, pv.Entlastung9bEur, 2);
            Assert.Equal(22914.0, pv.WirksamEur, 2);

            // … und zusammen 316.159,6 €/a — die Zahl der Abnahme.
            Assert.Equal(316159.6, bhkw.WirksamEur + pv.WirksamEur, 2);

            // Verteilt, nicht gerechnet: die Summe ist die Ausgangsgröße. Sechs
            // Nachkommastellen liegen vier Größenordnungen unter dem letzten Bit der
            // Beträge — gemessen wird die REGEL, nicht die Gleitkommaarithmetik.
            Assert.Equal(MENGE, bhkw.MengeMWh + pv.MengeMWh, 6);
            Assert.Equal(ARBEIT, bhkw.ArbeitEur + pv.ArbeitEur, 6);
            Assert.Equal(ENTLASTUNG, bhkw.Entlastung9bEur + pv.Entlastung9bEur, 6);

            // Zwei Anlagen teilen — der Schlüssel ist die Näherung V-4 und sagt es.
            Assert.True(bhkw.IstNaeherung);
            Assert.True(pv.IstNaeherung);
        }

        /// <summary>
        /// Bei genau EINER Anlage ist die Aufteilung exakt — sie bekommt alles und
        /// trägt kein Näherungskennzeichen. Ohne Eigenverbrauch gibt es gar keine
        /// Aufteilung; eine ohne Schlüssel wäre eine Behauptung.
        /// </summary>
        [Fact]
        public void Eine_Anlage_bekommt_alles_und_ohne_Schluessel_gibt_es_keine_Aufteilung()
        {
            List<VermiedenAnlageNachweis> eine = VermiedenAnlageNachweis.Verteile(
                new List<VermiedenAnlageNachweis>
                {
                    new VermiedenAnlageNachweis
                    { Komponente = WirtZeile.KOMPONENTE_BHKW, EigenMWh = 20 }
                },
                20, 4662, 400);

            Assert.Single(eine);
            Assert.Equal(1.0, eine[0].Anteil, 12);
            Assert.Equal(4662.0, eine[0].ArbeitEur, 6);
            Assert.Equal(4262.0, eine[0].WirksamEur, 6);
            Assert.False(eine[0].IstNaeherung);

            Assert.Empty(VermiedenAnlageNachweis.Verteile(
                new List<VermiedenAnlageNachweis>
                {
                    new VermiedenAnlageNachweis
                    { Komponente = WirtZeile.KOMPONENTE_BHKW, EigenMWh = 0 }
                },
                20, 4662, 400));
            Assert.Empty(VermiedenAnlageNachweis.Verteile(null, 20, 4662, 400));
        }

        /// <summary>
        /// <b>Die Gegenprobe zum ganzen Umbau:</b> Die Zwischensummen der
        /// Komponentenblöcke ergeben zusammen GENAU die Blocksumme A — dieselbe Zahl
        /// wie vor U6. Die Gliederung ordnet, sie rechnet nicht.
        /// </summary>
        [Fact]
        public void Die_Zwischensummen_ergeben_zusammen_die_Blocksumme_A()
        {
            WirtschaftlichkeitErgebnis e = Lauf();
            List<WirtZeile> zeilen = Rubrik(e);

            double teile = 0;
            int bloecke = 0;
            foreach (WirtZeile z in zeilen)
            {
                if (!z.IstTeilsumme || z.Block != WirtZeile.BLOCK_A) continue;
                bloecke++;
                teile += z.Wert(e).Value;
            }

            Assert.True(bloecke >= 2, "Die Rubrik führt keine zwei Komponentenblöcke — " +
                                      "der Prüffall misst nichts.");
            Assert.Equal(14575.0, teile, 6);
            Assert.Equal(Zeile(zeilen, "ERL_A_SUMME").Wert(e).Value, teile, 6);
        }

        /// <summary>
        /// Jede Zeile der Rubrik trägt ihren Anlagenbezug, und zwar den richtigen:
        /// § 53/§ 53a den Brennstoff der Stromerzeugung (Blockheizkraftwerk), § 54 den
        /// Heizstoff (Kessel), § 9b den Netzbezug (projektweit — er hängt am Restbezug,
        /// nicht an einer Anlage).
        /// </summary>
        [Fact]
        public void Jede_Zeile_traegt_ihren_Anlagenbezug()
        {
            List<WirtZeile> zeilen = Rubrik(Lauf());

            Assert.Equal(WirtZeile.KOMPONENTE_BHKW, Zeile(zeilen, "ERL_A_KWKG").Komponente);
            Assert.Equal(WirtZeile.KOMPONENTE_BHKW,
                         Zeile(zeilen, "ERL_A_ENERGIESTEUER").Komponente);
            Assert.Equal(WirtZeile.KOMPONENTE_KESSEL,
                         Zeile(zeilen, "ERL_A_ENERGIESTEUER_54").Komponente);
            Assert.Equal(WirtZeile.KOMPONENTE_PROJEKTWEIT,
                         Zeile(zeilen, "ERL_A_STROMST_ENTLASTUNG").Komponente);

            // Und die Köpfe stehen in der Reihenfolge der Rubrik: BHKW, Kessel,
            // projektweit — „projektweit" zuletzt.
            var koepfe = new List<string>();
            foreach (WirtZeile z in zeilen)
                if (z.IstKomponentenkopf && z.Block == WirtZeile.BLOCK_A)
                    koepfe.Add(z.Komponente);
            Assert.Equal(new[] { WirtZeile.KOMPONENTE_BHKW, WirtZeile.KOMPONENTE_KESSEL,
                                 WirtZeile.KOMPONENTE_PROJEKTWEIT }, koepfe);
        }

        /// <summary>
        /// Der LEISTUNGSANTEIL bleibt projektweit (Anwenderentscheid Q15): Er hängt an
        /// der Bezugsspitze des ganzen Projekts. In keinem Komponentenblock steht er.
        /// Fehlt die Spitze, steht dort eine Nullzeile MIT Grund statt einer stillen 0.
        /// </summary>
        [Fact]
        public void Der_Leistungsanteil_bleibt_projektweit_und_nennt_die_fehlende_Spitze()
        {
            WirtschaftlichkeitErgebnis e = MitAufteilung();
            List<WirtZeile> zeilen = Rubrik(e);

            WirtZeile leistung = Zeile(zeilen, "VERMIEDEN_LEISTUNG");
            Assert.NotNull(leistung);
            Assert.Equal(WirtZeile.KOMPONENTE_PROJEKTWEIT, leistung.Komponente);
            Assert.Equal(WirtZeile.BLOCK_B, leistung.Block);

            // Ohne gerechnete Bezugsspitze: 0 mit Grund, nicht 0 ohne Auskunft.
            Assert.Null(e.BezugsspitzeKW);
            string anzeige = leistung.Anzeige(e, CultureInfo.GetCultureInfo("de-DE"));
            Assert.Contains("Bezugsspitze", anzeige);

            // Mit gerechneter Spitze steht der Betrag da, ohne Zusatz.
            e.BezugsspitzeKW = 480.0;
            e.VermiedenLeistungJahr = -4180.0;
            WirtZeile mitSpitze = Zeile(Rubrik(e), "VERMIEDEN_LEISTUNG");
            Assert.DoesNotContain("Bezugsspitze",
                                  mitSpitze.Anzeige(e, CultureInfo.GetCultureInfo("de-DE")));
            Assert.Equal(-4180.0, mitSpitze.Wert(e).Value, 6);
        }

        /// <summary>
        /// Mit Aufteilung zerfällt der Ausweis in Komponentenblöcke: je Anlage der
        /// Bruttobetrag, der Abzug und die Abschlusszeile „vermiedene Kosten wirksam".
        /// Die eine projektweite Kette von vor U6 steht dann NICHT mehr daneben — sonst
        /// stünde derselbe Betrag zweimal im Block.
        /// </summary>
        [Fact]
        public void Mit_Aufteilung_zerfaellt_der_Ausweis_in_Komponentenbloecke()
        {
            WirtschaftlichkeitErgebnis e = MitAufteilung();
            List<WirtZeile> zeilen = Rubrik(e);

            Assert.Equal(315129.6, Zeile(zeilen, "VERMIEDEN_BRUTTO_BHKW").Wert(e).Value, 2);
            Assert.Equal(-21884.0, Zeile(zeilen, "ERL_B1_ABZUG_9B_BHKW").Wert(e).Value, 2);
            Assert.Equal(293245.6, Zeile(zeilen, "ERL_B1_EFFEKTIV_BHKW").Wert(e).Value, 2);
            Assert.Equal(24624.0, Zeile(zeilen, "VERMIEDEN_BRUTTO_PV").Wert(e).Value, 2);
            Assert.Equal(22914.0, Zeile(zeilen, "ERL_B1_EFFEKTIV_PV").Wert(e).Value, 2);

            // Die projektweite Kette von vor U6 ist verschwunden.
            Assert.Null(Zeile(zeilen, "VERMIEDEN_GESAMT"));
            Assert.Null(Zeile(zeilen, "ERL_B1_EFFEKTIV"));

            // Die Abschlusszeilen sind KEINE Blocksumme — Block B wird nicht summiert.
            foreach (WirtZeile z in zeilen)
                if (z.Block == WirtZeile.BLOCK_B && z.IstSumme) Assert.True(z.IstTeilsumme);
        }

        /// <summary>
        /// Die Herleitungszeile unter dem Bruttobetrag nennt Menge und Anteil — und das
        /// Wort „Näherung", sobald mehr als eine Anlage teilt (Entscheid A12). Als
        /// TEXTzeile erreicht sie auch Excel, ohne dort eine Wertspalte zu verderben.
        /// </summary>
        [Fact]
        public void Die_Herleitung_nennt_den_Anteil_und_die_Naeherung()
        {
            WirtschaftlichkeitErgebnis e = MitAufteilung();
            WirtZeile herleitung = Zeile(Rubrik(e), "VERMIEDEN_HERLEITUNG_PV");

            Assert.NotNull(herleitung);
            Assert.True(herleitung.IstText);
            string text = herleitung.Text(e);
            Assert.Contains("85,5", text);
            Assert.Contains("Näherung", text);
            // E7 (Konzept § 6.3 Nr. 32): Der Schlüssel ist der Eigenverbrauch je Anlage,
            // brutto aus der Strommatrix — ALT hieß es „nach dem Netto-Stromanteil".
            Assert.Contains("verteilt nach dem Eigenverbrauch je Anlage", text);
            Assert.Null(herleitung.ExcelWert(e));       // keine Zahl in der Wertspalte
        }

        /// <summary>
        /// <b>E7 (Konzept § 6.3 Nr. 32, Orchestrator-Entscheid 23.09.2026, Frage 6):</b>
        /// Im Rollentarif trägt die Aufteilung einen PV-Anteil — er ERSETZT die Zeile
        /// „PV: vermiedener Bezug" zum Flat-Preis. ALT standen beide untereinander
        /// (22.914,0 wirksam aus dem Anteil und 24.624,0 aus dem Flat-Preis); NEU steht
        /// nur der Anteil. Der Wert selbst bleibt am Ergebnis.
        /// </summary>
        [Fact]
        public void Im_Rollentarif_ersetzt_der_PV_Anteil_die_Zeile_vermiedener_Bezug()
        {
            WirtschaftlichkeitErgebnis e = MitAufteilung();
            e.PvVermiedenerBezug = 85.5 * 1000.0 * 0.2880;         // 24.624,0 €/a

            List<WirtZeile> zeilen = Rubrik(e);

            Assert.NotNull(Zeile(zeilen, "VERMIEDEN_BRUTTO_PV"));
            Assert.Null(Zeile(zeilen, "PV_VERMIEDEN"));
            Assert.Equal(24624.0, e.PvVermiedenerBezug.Value, 6);    // gerechnet bleibt er
        }

        /// <summary>
        /// Im Flat-Tarif gibt es keine Aufteilung der vermiedenen Kosten — die Zeile
        /// „PV: vermiedener Bezug" bleibt der Ausweis der Photovoltaik.
        /// </summary>
        [Fact]
        public void Im_Flat_Tarif_bleibt_die_Zeile_vermiedener_Bezug()
        {
            WirtschaftlichkeitErgebnis e = Lauf();
            e.PvVermiedenerBezug = 24624.0;
            Assert.Empty(e.VermiedenJeAnlage);

            WirtZeile z = Zeile(Rubrik(e), "PV_VERMIEDEN");

            Assert.NotNull(z);
            Assert.Equal(WirtZeile.KOMPONENTE_PV, z.Komponente);
            Assert.Equal(24624.0, z.Wert(e).Value, 6);
        }

        /// <summary>
        /// DER RÜCKFALL: Ein vor U6 gebuchter Stand trägt keine Aufteilung. Dann bleibt
        /// es bei der einen projektweiten Kette — Komponentenblöcke ohne Zahlen wären
        /// eine Behauptung. Genau dieses Verhalten zeigt <c>Lauf()</c>, und alle
        /// B7-Prüfstände darüber messen weiterhin dasselbe.
        /// </summary>
        [Fact]
        public void Ohne_Aufteilung_bleibt_es_bei_der_einen_projektweiten_Kette()
        {
            WirtschaftlichkeitErgebnis e = Lauf();
            Assert.Empty(e.VermiedenJeAnlage);

            List<WirtZeile> zeilen = Rubrik(e);
            Assert.NotNull(Zeile(zeilen, "VERMIEDEN_GESAMT"));
            Assert.NotNull(Zeile(zeilen, "ERL_B1_EFFEKTIV"));
            Assert.Null(Zeile(zeilen, "VERMIEDEN_BRUTTO_BHKW"));
            Assert.Equal(WirtZeile.KOMPONENTE_PROJEKTWEIT,
                         Zeile(zeilen, "VERMIEDEN_GESAMT").Komponente);
        }

        /// <summary>
        /// Die neuen Kopf- und Summenzeilen lassen die Wertspalten von Excel NUMERISCH:
        /// Ein Komponentenkopf trägt gar keinen Wert, eine Zwischensumme eine blanke
        /// Zahl. Das ist dieselbe Regel, an der die Herkunftszeilen aus U23 hängen.
        /// </summary>
        [Fact]
        public void Kopf_und_Zwischensumme_lassen_die_Excel_Wertspalten_numerisch()
        {
            WirtschaftlichkeitErgebnis e = Lauf();
            int koepfe = 0, summen = 0;
            foreach (WirtZeile z in Rubrik(e))
            {
                if (z.IstKomponentenkopf) { koepfe++; Assert.Null(z.ExcelWert(e)); }
                else if (z.IstTeilsumme)
                {
                    summen++;
                    Assert.False(z.IstText);
                    Assert.True(z.ExcelWert(e).HasValue);
                }
            }
            Assert.True(koepfe >= 2 && summen >= 2,
                        "Die Rubrik führt keine zwei Komponentenblöcke — der Prüffall misst nichts.");
        }

        /// <summary>
        /// Ein Lauf MIT Aufteilung: das Beispiel der Kategorie 7, auf die Größen des
        /// Kerns gelegt — 1.179,7 MWh vermiedener Bezug zu 28,80 ct/kWh, davon
        /// 85,5 MWh Photovoltaik, § 9b mit 20,00 €/MWh.
        /// </summary>
        private static WirtschaftlichkeitErgebnis MitAufteilung()
        {
            const double MENGE = 1179.7;
            const double PV_MWH = 85.5;
            var e = new WirtschaftlichkeitErgebnis
            {
                IdProjekt = 1,
                Anzeige = "Beide Anlagen",
                Szenario = WirtschaftlichkeitSzenario.ERWARTET,
                ProduzierendesGewerbe = true,
                VermiedenArbeitJahr = MENGE * 1000.0 * 0.2880,
                VermiedenLeistungJahr = 0,
                VermiedenGesamtJahr = MENGE * 1000.0 * 0.2880,
                VermiedenMengeMWh = MENGE,
                VermiedenEntlastung9bJahr = SATZ_9B_EUR_MWH * MENGE
            };
            e.VermiedenJeAnlage = VermiedenAnlageNachweis.Verteile(
                new List<VermiedenAnlageNachweis>
                {
                    new VermiedenAnlageNachweis
                    { Komponente = WirtZeile.KOMPONENTE_BHKW, EigenMWh = MENGE - PV_MWH },
                    new VermiedenAnlageNachweis
                    { Komponente = WirtZeile.KOMPONENTE_PV, EigenMWh = PV_MWH }
                },
                e.VermiedenMengeMWh, e.VermiedenArbeitJahr, e.VermiedenEntlastung9bJahr);
            return e;
        }

        /// <summary>
        /// Das Beispielprojekt der Kategorie 7 als Ergebniszeile — BHKW nach
        /// § 53a Abs. 5, Kessel nach § 54, Sockelbetrag 250 €/a. Die Zahlen stammen
        /// aus dem Rechenweg <c>05_Verguetungen_BHKW.md</c> bzw.
        /// <c>07_Erloesrubrik.md</c>; der ungerundete Brennwertfaktor 11,6/10,5 des
        /// Kerns ergibt 21.202,71 statt der 21.203,4 des Mockups (Befund B4).
        /// </summary>
        private static WirtschaftlichkeitErgebnis Beispielprojekt()
        {
            return new WirtschaftlichkeitErgebnis
            {
                IdProjekt = 1,
                Anzeige = "Beide Anlagen",
                KwkgVbhElektrisch = 5500,
                ProduzierendesGewerbe = true,
                EnergiesteuerJahr1 = 21202.71 + 2885.72,
                EnergiesteuerAufgeteilt = true,
                Energiesteuer53Jahr1 = 21202.71,
                Energiesteuer54Jahr1 = 2885.72,
                Energiesteuer54SockelJahr1 = 250.0,
                EnergiesteuerNachweise = new List<EnergiesteuerNachweis>
                {
                    new EnergiesteuerNachweis
                    {
                        Anlage = "BHKW", Paragraf = EnergiesteuerNachweis.PARAGRAF_53A,
                        Menge = 4796.99, Einheit = DbWerte.GESETZ_EINHEIT_EUR_MWH,
                        SatzEur = 4.42, BetragEur = 21202.71
                    },
                    new EnergiesteuerNachweis
                    {
                        Anlage = "Gas-Brennwertkessel", Paragraf = EnergiesteuerNachweis.PARAGRAF_54,
                        Menge = 2272.26, Einheit = DbWerte.GESETZ_EINHEIT_EUR_MWH,
                        SatzEur = 1.38, BetragEur = 3135.72
                    }
                }
            };
        }
    }
}
