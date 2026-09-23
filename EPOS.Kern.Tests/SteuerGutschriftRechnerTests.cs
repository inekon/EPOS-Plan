using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ETAPPE E1 — <b>der <see cref="SteuerGutschriftRechner"/></b>
    /// (Analysepapier 2026-09-19, Befund N3: „keine Testklasse").
    ///
    /// <para>Der Rechner führt drei Entlastungen der Energiesteuer (§§ 53, 53a, 54
    /// EnergieStG) und zwei der Stromsteuer (§ 9 Abs. 1 Nr. 3 Befreiung, § 9b
    /// Entlastung). Er ist REIN — Katalogsätze kommen als Delegat herein, es gibt
    /// keinen Datenbankzugriff; deshalb kommt diese Klasse ohne
    /// <c>TestDatenbank</c> aus und nimmt die Sätze aus
    /// <see cref="GesetzKatalog.Vorbelegung"/>.</para>
    ///
    /// <para><b>Die Sollwerte</b> sind die elf Handproben des Rechenwegs
    /// <c>05_Verguetungen_BHKW.md</c>, bestätigt in
    /// <c>Pruefung_Mockups_2026-09-19/01_Nachrechnung.md § 3</c>. Sie sind hier
    /// gemessen und getroffen: § 53a 21.202,71 €, § 53 26.383,46 €, § 54 6.369,85 €,
    /// § 9 Abs. 1 Nr. 3 23.677,50 €, § 9b 4.750,00 €.</para>
    ///
    /// <para><b>Hinweis zu den Mockup-Zahlen (Befund B4).</b> Der Mockup-Text zeigt
    /// 21.203,4 / 26.384,3 / 6.370,1 €. Diese Zahlen entstehen aus dem GERUNDETEN
    /// Brennwertfaktor 1,1048; der Kern rechnet mit 11,6/10,5 = 1,104762 durch und
    /// kommt auf die Werte oben. Gepinnt ist der Kern.</para>
    ///
    /// <para><b>Nicht hier:</b> der Modus AUSWEIS/ERLOES der § 9-Befreiung. Er ist
    /// keine Größe dieses Rechners — der liefert den Betrag, die Entscheidung, ob er
    /// in den Kapitalwert eingeht, trifft <c>WirtschaftlichkeitCtrl</c>. Dafür gibt
    /// es <c>StromsteuerBefreiungModusTests</c>. Der Fall
    /// <see cref="Der_Rechner_kennt_den_Modus_der_Befreiung_nicht"/> hält diese
    /// Arbeitsteilung fest.</para>
    /// </summary>
    public class SteuerGutschriftRechnerTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static readonly CultureInfo DE = CultureInfo.GetCultureInfo("de-DE");

        private const int JAHR = 2026;

        // ---- Das Beispielprojekt des Rechenwegs 05 --------------------------
        private const double PEL_KW = 300.0;
        private const double STROM_MWH = 1650.0;
        private const double ETA_EL = 0.38, ETA_TH = 0.45;
        private const double HI = 10.5, HS = 11.6;

        /// <summary>4.342,105… MWh Heizwert — 1.650 MWh Strom bei 38 % elektrisch.</summary>
        private static double BrennstoffHi => STROM_MWH / ETA_EL;

        /// <summary>1.953,947… MWh Nutzwärme.</summary>
        private static double WaermeMWh => BrennstoffHi * ETA_TH;

        // =====================================================================
        //  Der Katalog als Delegat
        // =====================================================================

        private static Func<string, GesetzParameter> Saetze(int jahr)
        {
            IList<GesetzParameter> vor = GesetzKatalog.Vorbelegung();
            return s => vor.Where(p => p.Schluessel == s && p.JahrVon <= jahr)
                           .OrderByDescending(p => p.JahrVon)
                           .FirstOrDefault();
        }

        private static SteuerErgebnis Rechne(SteuerEingabe e)
        {
            return SteuerGutschriftRechner.Rechne(e, JAHR, Saetze(JAHR), DE);
        }

        private static SteuerAnlage Bhkw(string wahl)
        {
            return new SteuerAnlage
            {
                Bezeichner = "BHKW",
                PelKW = PEL_KW,
                BrennstoffMWh = BrennstoffHi,
                StromMWh = STROM_MWH,
                WaermeMWh = WaermeMWh,
                SchluesselSatzVoll = DbWerte.GESETZ_ENERGIEST_ERDGAS,
                SchluesselSatz53a = DbWerte.GESETZ_ENERGIEST_53A5_ERDGAS,
                SchluesselSatz54 = DbWerte.GESETZ_ENERGIEST_54_ERDGAS,
                SchluesselCo2 = DbWerte.GESETZ_EF_BILANZ_EBEV_ERDGAS_HI,
                EffHi = HI,
                EffHs = HS,
                Fossil = true,
                Stromerzeuger = true,
                EnergiesteuerWahl = wahl,
            };
        }

        private static SteuerEingabe Projekt(params SteuerAnlage[] anlagen)
        {
            var e = new SteuerEingabe { Unternehmensart = DbWerte.UNTERNEHMENSART_PROD_GEWERBE };
            foreach (SteuerAnlage a in anlagen) e.Anlagen.Add(a);
            return e;
        }

        // =====================================================================
        //  1 — Energiesteuer § 53
        // =====================================================================

        /// <summary>
        /// § 53 Abs. 2: die Menge ist der UNGETEILTE Brennstoffeinsatz. Der Satz
        /// steht in €/MWh und bemisst sich am BRENNWERT — 4.342,105 MWh Hi werden
        /// über 11,6/10,5 zu 4.796,99 MWh Hs, mal 5,50 €/MWh.
        /// </summary>
        [Fact]
        public void Paragraf_53_voller_Brennstoff_entlastet_die_ganze_Menge()
        {
            SteuerEingabe e = Projekt(Bhkw(DbWerte.ENERGIESTEUER_WAHL_53));
            e.AufteilungMethode = DbWerte.AUFTEILUNG_VOLLER_BRENNSTOFF;

            SteuerErgebnis r = Rechne(e);

            Assert.Equal(26383.46, r.EnergiesteuerEur, 2);
            Assert.NotEmpty(r.Herkunft);
        }

        /// <summary>
        /// § 53 ENERGETISCH — die bewusste Untergrenze ohne Rechtsverfahren: nur der
        /// Stromanteil 1.650/(1.650+1.953,947) = 0,457831 der Menge.
        /// </summary>
        [Fact]
        public void Paragraf_53_energetisch_entlastet_nur_den_Stromanteil()
        {
            SteuerEingabe e = Projekt(Bhkw(DbWerte.ENERGIESTEUER_WAHL_53));
            e.AufteilungMethode = DbWerte.AUFTEILUNG_ENERGETISCH;

            SteuerErgebnis r = Rechne(e);

            Assert.Equal(12079.17, r.EnergiesteuerEur, 2);

            // Der energetische Weg ist stets kleiner als der volle.
            SteuerEingabe voll = Projekt(Bhkw(DbWerte.ENERGIESTEUER_WAHL_53));
            voll.AufteilungMethode = DbWerte.AUFTEILUNG_VOLLER_BRENNSTOFF;
            Assert.True(r.EnergiesteuerEur < Rechne(voll).EnergiesteuerEur);
        }

        // =====================================================================
        //  2 — Energiesteuer § 53a mit der Nutzungsgradschwelle
        // =====================================================================

        /// <summary>§ 53a Abs. 5: 4.796,99 MWh Hs × 4,42 €/MWh, Nutzungsgrad 83 % ≥ 70 %.</summary>
        [Fact]
        public void Paragraf_53a_entlastet_wenn_der_Nutzungsgrad_die_Schwelle_erreicht()
        {
            SteuerEingabe e = Projekt(Bhkw(DbWerte.ENERGIESTEUER_WAHL_53A));
            e.JahresnutzungsgradProzent = 83.0;

            Assert.Equal(21202.71, Rechne(e).EnergiesteuerEur, 2);
        }

        /// <summary>
        /// Unter der Schwelle gibt es 0 € — aber BENANNT, nicht still, und ohne
        /// Abbruch. Dasselbe gilt für einen ungepflegten Nutzungsgrad.
        /// </summary>
        [Theory]
        [InlineData(69.9)]
        [InlineData(65.0)]
        [InlineData(null)]
        public void Paragraf_53a_gibt_null_mit_Begruendung_unter_der_Schwelle(double? nutzungsgrad)
        {
            SteuerEingabe e = Projekt(Bhkw(DbWerte.ENERGIESTEUER_WAHL_53A));
            e.JahresnutzungsgradProzent = nutzungsgrad;

            SteuerErgebnis r = Rechne(e);

            Assert.Equal(0.0, r.EnergiesteuerEur, 6);
            Assert.Contains(r.Begruendungen, g => g.Contains("53a"));
        }

        /// <summary>Genau auf der Schwelle wird entlastet — 70 % ist „mindestens".</summary>
        [Fact]
        public void Paragraf_53a_entlastet_genau_auf_der_Schwelle()
        {
            SteuerEingabe e = Projekt(Bhkw(DbWerte.ENERGIESTEUER_WAHL_53A));
            e.JahresnutzungsgradProzent = 70.0;

            Assert.Equal(21202.71, Rechne(e).EnergiesteuerEur, 2);
        }

        // =====================================================================
        //  3 — Energiesteuer § 54 mit Sockel und Gewerbebedingung
        // =====================================================================

        /// <summary>§ 54: 4.796,99 MWh × 1,38 €/MWh − 250 € Sockel.</summary>
        [Fact]
        public void Paragraf_54_zieht_den_Sockel_von_250_Euro_ab()
        {
            Assert.Equal(6369.85, Rechne(Projekt(Bhkw(DbWerte.ENERGIESTEUER_WAHL_54))).EnergiesteuerEur, 2);
        }

        /// <summary>
        /// Der Sockel gilt EINMAL je Lauf, nicht je Anlage: zwei gleiche Anlagen
        /// ergeben das Doppelte des Bruttobetrags minus EINEN Sockel.
        /// </summary>
        [Fact]
        public void Der_Sockel_des_Paragrafen_54_faellt_einmal_je_Lauf()
        {
            double einzeln = Rechne(Projekt(Bhkw(DbWerte.ENERGIESTEUER_WAHL_54))).EnergiesteuerEur;
            double brutto = einzeln + 250.0;

            double zwei = Rechne(Projekt(Bhkw(DbWerte.ENERGIESTEUER_WAHL_54),
                                         Bhkw(DbWerte.ENERGIESTEUER_WAHL_54))).EnergiesteuerEur;

            Assert.Equal(2.0 * brutto - 250.0, zwei, 2);
            Assert.NotEqual(2.0 * einzeln, zwei, 2);
        }

        /// <summary>
        /// § 54 setzt produzierendes Gewerbe oder Land-/Forstwirtschaft voraus.
        /// Ohne das: 0 € mit Begründung.
        /// </summary>
        [Theory]
        [InlineData("PROD_GEWERBE", true)]
        [InlineData("LAND_FORSTWIRTSCHAFT", true)]
        [InlineData("KEIN_PROD_GEWERBE", false)]
        public void Paragraf_54_gilt_nur_fuer_produzierendes_Gewerbe(string art, bool entlastet)
        {
            SteuerEingabe e = Projekt(Bhkw(DbWerte.ENERGIESTEUER_WAHL_54));
            e.Unternehmensart = art;

            SteuerErgebnis r = Rechne(e);

            if (entlastet)
            {
                Assert.Equal(6369.85, r.EnergiesteuerEur, 2);
            }
            else
            {
                Assert.Equal(0.0, r.EnergiesteuerEur, 6);
                Assert.Contains(r.Begruendungen, g => g.Contains("54"));
            }
        }

        // =====================================================================
        //  4 — Die beiden Null-Fälle mit Begründung
        // =====================================================================

        /// <summary>
        /// §§ 53 und 53a entlasten nur Anlagen MIT Stromerzeugung. Ein Kessel bekommt
        /// 0 € und den Hinweis, dass allein § 54 in Betracht kommt (Ressourcentext
        /// <c>STEUER_ENERGIEST_NUR_54</c>).
        /// </summary>
        [Theory]
        [InlineData("PARAGRAF_53")]
        [InlineData("PARAGRAF_53A")]
        public void Ein_Kessel_bekommt_nach_53_und_53a_nichts(string wahl)
        {
            SteuerAnlage kessel = Bhkw(wahl);
            kessel.Bezeichner = "Kessel";
            kessel.Stromerzeuger = false;
            kessel.StromMWh = 0.0;

            SteuerEingabe e = Projekt(kessel);
            e.JahresnutzungsgradProzent = 90.0;

            SteuerErgebnis r = Rechne(e);

            Assert.Equal(0.0, r.EnergiesteuerEur, 6);
            Assert.Contains(r.Begruendungen, g => g.Contains("54"));
        }

        /// <summary>
        /// Ein fehlender Katalogsatz ergibt 0 € mit Begründung — nie einen geratenen
        /// Wert. Die Begründung nennt den Schlüssel, damit man weiß, was zu pflegen ist.
        /// </summary>
        [Fact]
        public void Ein_fehlender_Katalogsatz_ergibt_null_mit_Begruendung()
        {
            SteuerAnlage a = Bhkw(DbWerte.ENERGIESTEUER_WAHL_53);
            a.SchluesselSatzVoll = "GIBT_ES_NICHT";

            SteuerEingabe e = Projekt(a);
            e.AufteilungMethode = DbWerte.AUFTEILUNG_VOLLER_BRENNSTOFF;

            SteuerErgebnis r = Rechne(e);

            Assert.Equal(0.0, r.EnergiesteuerEur, 6);
            Assert.Contains(r.Begruendungen, g => g.Contains("GIBT_ES_NICHT"));
        }

        // =====================================================================
        //  5 — Die Einheitenkette
        // =====================================================================

        /// <summary>
        /// €/MWh: die Menge wird über Hs/Hi auf den Brennwert gehoben. Ohne
        /// gepflegten Brennwert bleibt es KONSERVATIV beim Heizwert — die Entlastung
        /// fällt dann rund 10 % zu niedrig aus, mit Begründung.
        /// </summary>
        [Fact]
        public void Einheit_Euro_je_MWh_rechnet_ueber_Hs_durch_Hi()
        {
            SteuerEingabe mit = Projekt(Bhkw(DbWerte.ENERGIESTEUER_WAHL_53));
            mit.AufteilungMethode = DbWerte.AUFTEILUNG_VOLLER_BRENNSTOFF;
            Assert.Equal(BrennstoffHi * (HS / HI) * 5.50, Rechne(mit).EnergiesteuerEur, 2);

            SteuerAnlage ohne = Bhkw(DbWerte.ENERGIESTEUER_WAHL_53);
            ohne.EffHs = 0.0;
            SteuerEingabe eOhne = Projekt(ohne);
            eOhne.AufteilungMethode = DbWerte.AUFTEILUNG_VOLLER_BRENNSTOFF;

            SteuerErgebnis r = Rechne(eOhne);
            Assert.Equal(BrennstoffHi * 5.50, r.EnergiesteuerEur, 2);
            Assert.NotEmpty(r.Begruendungen);
        }

        /// <summary>
        /// €/1.000 l — Menge = MWh × 1.000 / Hi / 1.000. Der Träger muss je LITER
        /// abgerechnet sein; eine Dichte wird nie geraten.
        /// </summary>
        [Fact]
        public void Einheit_Euro_je_1000_Liter_rechnet_ueber_den_Heizwert_je_Liter()
        {
            SteuerAnlage oel = Bhkw(DbWerte.ENERGIESTEUER_WAHL_53);
            oel.SchluesselSatzVoll = DbWerte.GESETZ_ENERGIEST_HEIZOEL_EL;   // 61,35 €/1000 l
            oel.Abrechnungseinheit = "l";
            oel.EffHi = 10.0;            // kWh je Liter
            oel.BrennstoffMWh = 1000.0;

            SteuerEingabe e = Projekt(oel);
            e.AufteilungMethode = DbWerte.AUFTEILUNG_VOLLER_BRENNSTOFF;

            // 1.000 MWh / 10 kWh je l = 100.000 l = 100 × 1.000 l
            Assert.Equal(100.0 * 61.35, Rechne(e).EnergiesteuerEur, 2);
        }

        /// <summary>€/1.000 kg — dasselbe je Kilogramm.</summary>
        [Fact]
        public void Einheit_Euro_je_1000_Kilogramm_rechnet_ueber_den_Heizwert_je_Kilogramm()
        {
            SteuerAnlage fluessig = Bhkw(DbWerte.ENERGIESTEUER_WAHL_53);
            fluessig.SchluesselSatzVoll = DbWerte.GESETZ_ENERGIEST_FLUESSIGGAS;  // 60,60 €/1000 kg
            fluessig.Abrechnungseinheit = "kg";
            fluessig.EffHi = 12.5;       // kWh je Kilogramm
            fluessig.BrennstoffMWh = 1000.0;

            SteuerEingabe e = Projekt(fluessig);
            e.AufteilungMethode = DbWerte.AUFTEILUNG_VOLLER_BRENNSTOFF;

            // 1.000 MWh / 12,5 kWh je kg = 80.000 kg = 80 × 1.000 kg
            Assert.Equal(80.0 * 60.60, Rechne(e).EnergiesteuerEur, 2);
        }

        /// <summary>
        /// Eine je Liter abgerechnete Menge lässt sich NICHT in Kilogramm umrechnen —
        /// die Dichte ist nirgends gepflegt. Lieber 0 € mit Begründung als eine
        /// geratene Dichte.
        /// </summary>
        [Fact]
        public void Ohne_passende_Abrechnungseinheit_gibt_es_null_mit_Begruendung()
        {
            SteuerAnlage a = Bhkw(DbWerte.ENERGIESTEUER_WAHL_53);
            a.SchluesselSatzVoll = DbWerte.GESETZ_ENERGIEST_FLUESSIGGAS;   // €/1.000 kg
            a.Abrechnungseinheit = "l";                                     // aber je Liter gemessen
            a.EffHi = 10.0;
            a.BrennstoffMWh = 1000.0;

            SteuerEingabe e = Projekt(a);
            e.AufteilungMethode = DbWerte.AUFTEILUNG_VOLLER_BRENNSTOFF;

            SteuerErgebnis r = Rechne(e);

            Assert.Equal(0.0, r.EnergiesteuerEur, 6);
            Assert.NotEmpty(r.Begruendungen);
        }

        /// <summary>€/GJ — Menge = MWh × 3,6, auf dem Heizwert.</summary>
        [Fact]
        public void Einheit_Euro_je_GJ_rechnet_mit_dem_Faktor_3_6()
        {
            SteuerAnlage kohle = Bhkw(DbWerte.ENERGIESTEUER_WAHL_53A);
            kohle.SchluesselSatz53a = DbWerte.GESETZ_ENERGIEST_53A5_KOHLE;  // 0,16 €/GJ
            kohle.BrennstoffMWh = 1000.0;

            SteuerEingabe e = Projekt(kohle);
            e.JahresnutzungsgradProzent = 80.0;

            // 1.000 MWh × 3,6 = 3.600 GJ × 0,16 €/GJ
            Assert.Equal(3600.0 * 0.16, Rechne(e).EnergiesteuerEur, 2);
        }

        // =====================================================================
        //  6 — Stromsteuer § 9 Abs. 1 Nr. 3
        // =====================================================================

        private static SteuerEingabe Stromsteuerfall()
        {
            SteuerEingabe e = Projekt(Bhkw(DbWerte.ENERGIESTEUER_WAHL_KEINE));
            e.RaeumlicherZusammenhang = true;
            e.HocheffizienzNachweis = true;
            e.KwkEigenMWh = 1155.0;
            e.NetzbezugMWh = 250.0;
            return e;
        }

        /// <summary>
        /// Alle vier Bedingungen erfüllt: 1.155,0 MWh × 20,50 €/MWh. Der
        /// CO₂-Energieertrag ist — brennwertbezogen, Konzept § 6.3 Nr. 29 —
        /// 181,4 × 4.342,105 / (1.650 + 1.953,947) = 218,55 g/kWh und damit unter dem
        /// Grenzwert von 270 (heizwertbezogen waren es 242,05 g/kWh; der Betrag bleibt).
        /// </summary>
        [Fact]
        public void Paragraf_9_Absatz_1_Nummer_3_befreit_den_KWK_Eigenverbrauch()
        {
            Assert.Equal(23677.50, Rechne(Stromsteuerfall()).StromsteuerBefreiungEur, 2);
        }

        /// <summary>
        /// Jede der vier Bedingungen EINZELN verletzt kostet die Befreiung ganz —
        /// und zwar mit Begründung. Die § 9b-Entlastung bleibt davon unberührt, die
        /// Mengen beider Vorschriften sind disjunkt.
        /// </summary>
        [Theory]
        [InlineData("hocheffizienz")]
        [InlineData("raeumlich")]
        [InlineData("leistung")]
        [InlineData("co2")]
        public void Jede_verletzte_Bedingung_kostet_die_Befreiung_des_Paragrafen_9(string welche)
        {
            SteuerEingabe e = Stromsteuerfall();
            switch (welche)
            {
                case "hocheffizienz": e.HocheffizienzNachweis = false; break;
                case "raeumlich": e.RaeumlicherZusammenhang = false; break;
                case "leistung": e.Anlagen[0].PelKW = 2500.0; break;   // über 2.000 kW
                // 266,4 × H_i/H_s (10,5/11,6) × 1,2048 = 290,5 g/kWh (H_s) → über 270
                case "co2": e.Anlagen[0].SchluesselCo2 =
                                DbWerte.GESETZ_EF_BILANZ_EBEV_HEIZOEL_EL; break;
            }

            SteuerErgebnis r = Rechne(e);

            Assert.Equal(0.0, r.StromsteuerBefreiungEur, 6);
            Assert.NotEmpty(r.Begruendungen);

            // § 9b bleibt unberührt.
            Assert.Equal(4750.00, r.StromsteuerEntlastungEur, 2);
        }

        /// <summary>Genau auf der Leistungsgrenze wird noch befreit — 2 MW ist „bis".</summary>
        [Fact]
        public void Genau_auf_der_Leistungsgrenze_wird_noch_befreit()
        {
            SteuerEingabe e = Stromsteuerfall();
            e.Anlagen[0].PelKW = 2000.0;

            Assert.Equal(23677.50, Rechne(e).StromsteuerBefreiungEur, 2);
        }

        /// <summary>
        /// Der Rechner liefert den BETRAG der Befreiung — er kennt den Modus
        /// AUSWEIS/ERLOES nicht. Die Entscheidung, ob der Betrag in den Kapitalwert
        /// eingeht, fällt eine Ebene höher (<c>WirtschaftlichkeitCtrl</c>,
        /// Nachweis in <c>StromsteuerBefreiungModusTests</c>). Diese Arbeitsteilung
        /// ist gewollt und wird hier festgehalten.
        /// </summary>
        [Fact]
        public void Der_Rechner_kennt_den_Modus_der_Befreiung_nicht()
        {
            SteuerErgebnis r = Rechne(Stromsteuerfall());

            Assert.Equal(23677.50, r.StromsteuerBefreiungEur, 2);

            // Die Summe des Rechners zählt die Befreiung MIT — die Rubrikentscheidung
            // trifft der Aufrufer, nicht dieser Rechner.
            Assert.Equal(r.EnergiesteuerEur + r.StromsteuerBefreiungEur + r.StromsteuerEntlastungEur,
                         r.SummeEur, 6);
        }

        // =====================================================================
        //  7 — Stromsteuer § 9b
        // =====================================================================

        /// <summary>§ 9b: max(0, 20,00 €/MWh × Netzbezug − 250 €/a).</summary>
        [Theory]
        [InlineData(250.0, 4750.00)]    // 250 × 20 − 250
        [InlineData(335.5, 6460.00)]    // Beleg des Rechenwegs: nur mit BHKW
        [InlineData(100.0, 1750.00)]
        [InlineData(12.5, 0.0)]         // genau der Sockel → nichts bleibt
        [InlineData(10.0, 0.0)]         // darunter → 0, nicht negativ
        [InlineData(0.0, 0.0)]
        public void Paragraf_9b_entlastet_den_Netzbezug_abzueglich_des_Sockels(
            double netzbezugMWh, double erwartet)
        {
            var e = new SteuerEingabe
            {
                Unternehmensart = DbWerte.UNTERNEHMENSART_PROD_GEWERBE,
                NetzbezugMWh = netzbezugMWh,
            };

            Assert.Equal(erwartet, Rechne(e).StromsteuerEntlastungEur, 2);
        }

        /// <summary>§ 9b hängt an keiner KWK-Anlage, aber am produzierenden Gewerbe.</summary>
        [Fact]
        public void Paragraf_9b_gilt_nur_fuer_produzierendes_Gewerbe()
        {
            var e = new SteuerEingabe
            {
                Unternehmensart = DbWerte.UNTERNEHMENSART_KEIN_PROD_GEWERBE,
                NetzbezugMWh = 250.0,
            };

            Assert.Equal(0.0, Rechne(e).StromsteuerEntlastungEur, 6);
        }

        // =====================================================================
        //  8 — Der Rechner ist gutmütig
        // =====================================================================

        /// <summary>Leere Eingaben werfen nicht, sie ergeben ein leeres Ergebnis.</summary>
        [Fact]
        public void Leere_Eingaben_ergeben_ein_leeres_Ergebnis_statt_einer_Ausnahme()
        {
            SteuerErgebnis ohneEingabe = SteuerGutschriftRechner.Rechne(null, JAHR, Saetze(JAHR), DE);
            Assert.Equal(0.0, ohneEingabe.SummeEur, 6);

            SteuerErgebnis ohneKatalog = SteuerGutschriftRechner.Rechne(Stromsteuerfall(), JAHR, null, DE);
            Assert.Equal(0.0, ohneKatalog.SummeEur, 6);
        }

        /// <summary>
        /// <see cref="SteuerGutschriftRechner.ProduzierendesGewerbe"/> ist die EINE
        /// Stelle, an der die Gewerbebedingung entschieden wird.
        /// </summary>
        [Theory]
        [InlineData("PROD_GEWERBE", true)]
        [InlineData("LAND_FORSTWIRTSCHAFT", true)]
        [InlineData("KEIN_PROD_GEWERBE", false)]
        [InlineData("", false)]
        public void ProduzierendesGewerbe_entscheidet_an_einer_Stelle(string art, bool erwartet)
        {
            Assert.Equal(erwartet, SteuerGutschriftRechner.ProduzierendesGewerbe(
                new SteuerEingabe { Unternehmensart = art }));
        }

        // =====================================================================
        //  9 — AUFTRAG U7: zwei Beträge statt einer Summe (Befund B7-1)
        // =====================================================================

        /// <summary>
        /// Der Kessel des Beispielprojekts (Mockup Kategorie 4 und 7): dieselbe
        /// Nutzwärme wie das BHKW, Kesselwirkungsgrad 95 %, Wahl § 54 <b>an der
        /// Anlage</b> — nicht die Projektvorgabe § 53a. Er erzeugt keinen Strom und
        /// kommt deshalb allein für § 54 in Betracht (BF5).
        /// </summary>
        private static SteuerAnlage Kessel()
        {
            return new SteuerAnlage
            {
                Bezeichner = "Gas-Brennwertkessel",
                PelKW = 0.0,
                BrennstoffMWh = WaermeMWh / 0.95,       // 2.056,79 MWh (H_i)
                StromMWh = 0.0,
                WaermeMWh = WaermeMWh,
                SchluesselSatzVoll = DbWerte.GESETZ_ENERGIEST_ERDGAS,
                SchluesselSatz53a = DbWerte.GESETZ_ENERGIEST_53A5_ERDGAS,
                SchluesselSatz54 = DbWerte.GESETZ_ENERGIEST_54_ERDGAS,
                SchluesselCo2 = DbWerte.GESETZ_EF_BILANZ_EBEV_ERDGAS_HI,
                EffHi = HI,
                EffHs = HS,
                Fossil = true,
                Stromerzeuger = false,
                EnergiesteuerWahl = DbWerte.ENERGIESTEUER_WAHL_54,
            };
        }

        /// <summary>
        /// <b>Die Zahlenprobe der Etappe (Mockup, Umsetzungsstand U7) — seit E7c
        /// GESPERRT (S‑2, Entscheid A3):</b> Am Beispielprojekt mit BHKW nach § 53a
        /// Abs. 5 UND Kessel nach § 54 stehen zwei Entlastungswelten nebeneinander.
        /// Diese Mischlage ist nicht zulässig; der § 54-Betrag wird verworfen, der
        /// § 53a-Teil bleibt.
        ///
        /// <para>Gemessen wird am KERN: 4.796,99 MWh (H_s) × 4,42 €/MWh = 21.202,71 €
        /// (§ 53a) — unverändert. Der § 54-Teil, 2.272,26 MWh (H_s) × 1,38 €/MWh =
        /// 3.135,72 € vor dem Sockel, ist verworfen: <b>alt 2.885,72 € (nach Sockel 250 €),
        /// neu 0 €</b>; die Summe <b>alt 24.088,43 €, neu 21.202,71 €</b>. Der Mockup nennt
        /// 21.203,4 — die Zahl des GERUNDETEN Brennwertfaktors 1,1048 (Befund B4, oben
        /// im Klassenkopf); gepinnt ist der ungerundete Quotient.</para>
        /// </summary>
        [Fact]
        public void Zahlenprobe_Die_Mischlage_aus_Paragraf_53a_und_Paragraf_54_ist_gesperrt()
        {
            SteuerEingabe e = Projekt(Bhkw(DbWerte.ENERGIESTEUER_WAHL_53A), Kessel());
            e.JahresnutzungsgradProzent = 83.0;

            SteuerErgebnis r = Rechne(e);

            Assert.Equal(21202.71, r.Energiesteuer53Eur, 2);
            Assert.Equal(0.0, r.Energiesteuer54Eur, 6);            // E7c S-2: alt 2.885,72
            Assert.Equal(0.0, r.Energiesteuer54SockelEur, 6);      // E7c S-2: alt 250,00
            Assert.True(r.Energiesteuer54Gesperrt);
            Assert.Equal(3135.72, r.Energiesteuer54VerworfenEur, 2);

            // Und die eine Zahl, die Erlösreihe, Ergebnisspalte und Anker lesen:
            Assert.Equal(21202.71, r.EnergiesteuerEur, 2);         // E7c S-2: alt 24.088,43
            Assert.Equal(r.Energiesteuer53Eur + r.Energiesteuer54Eur, r.EnergiesteuerEur, 9);

            // Die § 54-Zeile trägt die Begründung, die Liste ebenso.
            Assert.StartsWith("§ 54 EnergieStG gesperrt", r.PositionsGruende[SteuerPosition.ENERGIEST_54]);
            Assert.Contains(r.Begruendungen, b => b.StartsWith("§ 54 EnergieStG gesperrt", StringComparison.Ordinal));
        }

        /// <summary>
        /// Die Aufteilung selbst bleibt: Ohne Mischlage — der Kessel allein nach § 54 —
        /// trägt der § 54-Topf seinen Betrag nach Sockel, <b>2.885,72 €</b>.
        /// </summary>
        [Fact]
        public void Ohne_Mischlage_traegt_der_Kessel_seinen_Paragraf_54_Betrag()
        {
            SteuerErgebnis r = Rechne(Projekt(Kessel()));

            Assert.False(r.Energiesteuer54Gesperrt);
            Assert.Equal(0.0, r.Energiesteuer53Eur, 6);
            Assert.Equal(2885.72, r.Energiesteuer54Eur, 2);
            Assert.Equal(250.0, r.Energiesteuer54SockelEur, 6);
        }

        /// <summary>
        /// S‑2 (Entscheid A3): Die Sperre setzt an der WAHL an und gilt auch für § 53;
        /// ein Heizkessel mit § 53-Wahl begründet dagegen keine zweite Welt — § 53 kommt
        /// für ihn nie in Betracht (B3), seine Wahl rechnet 0 mit eigener Begründung.
        /// </summary>
        [Fact]
        public void Die_Mischlage_setzt_an_der_Wahl_an_und_nicht_am_Kessel_mit_Paragraf_53()
        {
            List<SteuerAnlage> strom, gewerbe;

            Assert.True(SteuerGutschriftRechner.Mischlage(
                Projekt(Bhkw(DbWerte.ENERGIESTEUER_WAHL_53), Kessel()), out strom, out gewerbe));
            Assert.Single(strom);
            Assert.Single(gewerbe);

            SteuerAnlage kessel53 = Kessel();
            kessel53.EnergiesteuerWahl = DbWerte.ENERGIESTEUER_WAHL_53;
            Assert.False(SteuerGutschriftRechner.Mischlage(
                Projekt(Bhkw(DbWerte.ENERGIESTEUER_WAHL_54), kessel53), out strom, out gewerbe));

            // Ohne Brennstoff keine Welt.
            SteuerAnlage leer = Kessel();
            leer.BrennstoffMWh = 0.0;
            Assert.False(SteuerGutschriftRechner.Mischlage(
                Projekt(Bhkw(DbWerte.ENERGIESTEUER_WAHL_53), leer), out strom, out gewerbe));

            // § 53 und § 54 im selben Projekt: § 53 bleibt, § 54 ist verworfen.
            SteuerErgebnis r = Rechne(Projekt(Bhkw(DbWerte.ENERGIESTEUER_WAHL_53), Kessel()));
            Assert.True(r.Energiesteuer54Gesperrt);
            Assert.Equal(26383.46, r.Energiesteuer53Eur, 2);
            Assert.Equal(0.0, r.Energiesteuer54Eur, 6);
            Assert.DoesNotContain(r.EnergiesteuerNachweise, n => n.Ist54);
        }

        /// <summary>
        /// DIE GEGENPROBE zur Aufteilung: Der Sockelbetrag des § 54 mindert
        /// ausschließlich den § 54-Teil. Ohne den Kessel wäre der § 53a-Betrag
        /// derselbe — eine Anlage zahlt nicht den Sockel einer anderen.
        /// </summary>
        [Fact]
        public void Der_Sockel_des_Paragrafen_54_laesst_den_Paragrafen_53a_unberuehrt()
        {
            SteuerEingabe allein = Projekt(Bhkw(DbWerte.ENERGIESTEUER_WAHL_53A));
            allein.JahresnutzungsgradProzent = 83.0;

            SteuerEingabe zusammen = Projekt(Bhkw(DbWerte.ENERGIESTEUER_WAHL_53A), Kessel());
            zusammen.JahresnutzungsgradProzent = 83.0;

            Assert.Equal(Rechne(allein).Energiesteuer53Eur, Rechne(zusammen).Energiesteuer53Eur, 6);
            Assert.Equal(0.0, Rechne(allein).Energiesteuer54Eur, 6);
        }

        /// <summary>
        /// Jede Wahl landet in IHREM Topf, und die Summe bleibt in jedem Fall die
        /// Zahl, die der Rechner vor U7 als einzige zurückgab. Die Sollwerte sind die
        /// der Fälle oben — hier steht, WELCHER der beiden Beträge sie trägt.
        /// </summary>
        [Theory]
        [InlineData("ENERGIESTEUER_WAHL_53", 26383.46, 0.0)]
        [InlineData("ENERGIESTEUER_WAHL_53A", 21202.71, 0.0)]
        [InlineData("ENERGIESTEUER_WAHL_54", 0.0, 6369.85)]
        public void Jede_Wahl_landet_in_ihrem_Betrag_und_die_Summe_bleibt(
            string wahlName, double erwartet53, double erwartet54)
        {
            string wahl = wahlName == "ENERGIESTEUER_WAHL_53" ? DbWerte.ENERGIESTEUER_WAHL_53
                        : wahlName == "ENERGIESTEUER_WAHL_53A" ? DbWerte.ENERGIESTEUER_WAHL_53A
                        : DbWerte.ENERGIESTEUER_WAHL_54;

            SteuerEingabe e = Projekt(Bhkw(wahl));
            e.AufteilungMethode = DbWerte.AUFTEILUNG_VOLLER_BRENNSTOFF;
            e.JahresnutzungsgradProzent = 83.0;

            SteuerErgebnis r = Rechne(e);

            Assert.Equal(erwartet53, r.Energiesteuer53Eur, 2);
            Assert.Equal(erwartet54, r.Energiesteuer54Eur, 2);
            Assert.Equal(erwartet53 + erwartet54, r.EnergiesteuerEur, 2);
        }

        /// <summary>
        /// Der Nachweis trägt je Position Paragraf, Menge in der gesetzlichen
        /// Einheit, Satz und Betrag — die drei Zahlen, aus denen die Rubrik ihre
        /// Herleitungszeile schreibt, ohne sie nachzurechnen.
        /// </summary>
        [Fact]
        public void Der_Nachweis_traegt_Paragraf_Menge_Satz_und_Betrag_je_Position()
        {
            // E7c (S-2): BHKW nach § 53a und Kessel nach § 54 im SELBEN Projekt sind
            // seither gesperrt (§ 54 verworfen, sein Posten verlässt den Nachweis). Die
            // zwei Nachweise stehen deshalb je in ihrem eigenen Projekt — die Zahlen sind
            // dieselben wie vorher.
            SteuerEingabe e = Projekt(Bhkw(DbWerte.ENERGIESTEUER_WAHL_53A));
            e.JahresnutzungsgradProzent = 83.0;

            SteuerErgebnis r = Rechne(e);
            SteuerErgebnis r54 = Rechne(Projekt(Kessel()));

            Assert.Single(r.EnergiesteuerNachweise);
            Assert.Single(r54.EnergiesteuerNachweise);

            EnergiesteuerNachweis n53 = r.EnergiesteuerNachweise
                .Single(n => n.Paragraf == EnergiesteuerNachweis.PARAGRAF_53A);
            Assert.Equal("BHKW", n53.Anlage);
            Assert.False(n53.Ist54);
            Assert.Equal(4796.99, n53.Menge, 2);              // brennwertbezogen
            Assert.Equal(DbWerte.GESETZ_EINHEIT_EUR_MWH, n53.Einheit);
            Assert.Equal(4.42, n53.SatzEur, 6);
            Assert.Equal(21202.71, n53.BetragEur, 2);

            EnergiesteuerNachweis n54 = r54.EnergiesteuerNachweise
                .Single(n => n.Paragraf == EnergiesteuerNachweis.PARAGRAF_54);
            Assert.True(n54.Ist54);
            Assert.Equal(2272.26, n54.Menge, 2);
            Assert.Equal(1.38, n54.SatzEur, 6);
            // Der Nachweis führt den Betrag VOR dem Sockel — der fällt einmal je
            // Lauf an und steht deshalb neben den Anlagenzeilen.
            Assert.Equal(3135.72, n54.BetragEur, 2);
            Assert.Equal(n54.BetragEur - r54.Energiesteuer54SockelEur, r54.Energiesteuer54Eur, 6);
        }

        /// <summary>
        /// Ohne gerechnete Entlastung bleibt der Nachweis leer — und mit ihm die
        /// Herleitungszeile der Rubrik. Eine Zeile ohne Zahlen wäre eine Behauptung.
        /// </summary>
        [Fact]
        public void Ohne_Entlastung_bleibt_der_Nachweis_leer()
        {
            SteuerEingabe e = Projekt(Bhkw(DbWerte.ENERGIESTEUER_WAHL_KEINE));

            SteuerErgebnis r = Rechne(e);

            Assert.Empty(r.EnergiesteuerNachweise);
            Assert.Equal(0.0, r.Energiesteuer53Eur, 6);
            Assert.Equal(0.0, r.Energiesteuer54Eur, 6);
            Assert.NotEmpty(r.Begruendungen);
        }

        // =====================================================================
        //  10 — AUFTRAG 9d: die Begründung JE POSITION (Befund B7-4)
        // =====================================================================

        /// <summary>
        /// Jede Begründung steht in <see cref="SteuerErgebnis.Begruendungen"/> wie
        /// bisher UND unter ihrer Position. Ohne die Zuordnung war aus dem flachen
        /// Hinweistext nicht mehr zu lesen, welche Zeile der Erlösrubrik gemeint ist.
        /// </summary>
        [Fact]
        public void Jede_Begruendung_steht_unter_ihrer_Position()
        {
            // § 54 ohne produzierendes Gewerbe, § 9b desgleichen, § 9 Abs. 1 Nr. 3
            // ohne Hocheffizienznachweis: drei Positionen, drei Gründe.
            SteuerEingabe e = Projekt(Bhkw(DbWerte.ENERGIESTEUER_WAHL_54));
            e.Unternehmensart = DbWerte.UNTERNEHMENSART_KEIN_PROD_GEWERBE;
            e.NetzbezugMWh = 250.0;
            e.HocheffizienzNachweis = false;

            SteuerErgebnis r = Rechne(e);

            Assert.Contains("54", r.PositionsGruende[SteuerPosition.ENERGIEST_54]);
            Assert.False(string.IsNullOrEmpty(r.PositionsGruende[SteuerPosition.STROMST_BEFREIUNG]));
            Assert.False(string.IsNullOrEmpty(r.PositionsGruende[SteuerPosition.STROMST_ENTLASTUNG]));

            // Die flache Liste bleibt, was sie war — sie speist unverändert das
            // Hinweisfeld des Laufs.
            foreach (string grund in r.PositionsGruende.Values)
                Assert.Contains(grund, r.Begruendungen);
        }

        /// <summary>
        /// Der Grund landet bei DER Vorschrift, an der die Rechnung ausgestiegen
        /// ist: Ein Kessel mit der Wahl § 53a begründet die § 53er Zeile, nicht die
        /// § 54er — dort ist nichts gewählt und also auch nichts festgestellt.
        /// </summary>
        [Fact]
        public void Der_Grund_landet_bei_der_Vorschrift_die_gescheitert_ist()
        {
            SteuerEingabe e = Projekt(Kessel());
            e.Anlagen[0].EnergiesteuerWahl = DbWerte.ENERGIESTEUER_WAHL_53A;
            e.JahresnutzungsgradProzent = 83.0;

            SteuerErgebnis r = Rechne(e);

            Assert.Contains("53", r.PositionsGruende[SteuerPosition.ENERGIEST_53]);
            Assert.False(r.PositionsGruende.ContainsKey(SteuerPosition.ENERGIEST_54));
        }

        /// <summary>
        /// Ist gar keine Entlastungsnorm gewählt, fehlt BEIDEN Paragrafenzeilen die
        /// Grundlage — der Satz steht deshalb an beiden.
        /// </summary>
        [Fact]
        public void Ohne_jede_Wahl_bekommen_beide_Paragrafenzeilen_denselben_Grund()
        {
            SteuerErgebnis r = Rechne(Projekt(Bhkw(DbWerte.ENERGIESTEUER_WAHL_KEINE)));

            Assert.Equal(r.PositionsGruende[SteuerPosition.ENERGIEST_53],
                         r.PositionsGruende[SteuerPosition.ENERGIEST_54]);
        }

        /// <summary>
        /// Der ERSTE Grund je Position gilt — er ist der, an dem die Rechnung
        /// ausgestiegen ist. Zwei gescheiterte Anlagen derselben Vorschrift melden
        /// deshalb EINEN Grund an die Zeile, aber ZWEI in die flache Liste.
        /// </summary>
        [Fact]
        public void Der_erste_Grund_je_Position_gilt()
        {
            SteuerAnlage a1 = Bhkw(DbWerte.ENERGIESTEUER_WAHL_53A);
            a1.Bezeichner = "BHKW 1";
            SteuerAnlage a2 = Bhkw(DbWerte.ENERGIESTEUER_WAHL_53A);
            a2.Bezeichner = "BHKW 2";
            a1.SchluesselSatz53a = "";          // Träger ohne Satz — beide scheitern
            a2.SchluesselSatz53a = "";

            SteuerEingabe e = Projekt(a1, a2);
            e.JahresnutzungsgradProzent = 83.0;

            SteuerErgebnis r = Rechne(e);

            Assert.Single(r.PositionsGruende.Keys.Where(k => k == SteuerPosition.ENERGIEST_53));
            Assert.Contains("BHKW 1", r.PositionsGruende[SteuerPosition.ENERGIEST_53]);
            Assert.Equal(2, r.Begruendungen.Count(g => g.Contains("BHKW")));
        }
    }
}
