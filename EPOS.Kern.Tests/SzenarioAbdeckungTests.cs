using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ETAPPE E9b (Konzept § 2.11.5 „Pflege", § 2.11.7; Entscheide E9b‑Q2 und E9b‑Q3,
    /// Lesart a) — <b>der Ausweis „n von m Parametern szenariert"</b> an der Stelle des
    /// früheren Hinweistexts, und die zwei Helfer der Szenariotafel im Kern.
    ///
    /// <para><b>Die Zählregel</b> (<see cref="SzenarioAbdeckung.Zaehle"/>): <c>m</c> sind die
    /// sieben Größen des W5‑B‑9-Satzes, Betrachtungszeitraum, Mengenänderung, die zwei
    /// Einspeisevergütungen, je Vergütungszeile eines PV-Standes DV-Entgelt und PPA-Preis
    /// und je Träger mit Verbrauch Arbeits- und Grundpreis (der Leistungspreis beim
    /// Stromträger immer, sonst nur, wo einer gepflegt ist). <c>n</c> zählt, was gepflegt
    /// ist UND vom Erwartet-Wert um mehr als 1e−9 abweicht — die Vorgabe (leeres Feld) der
    /// sieben Größen zählt nicht, die Nullregel der neuen Größen gilt.</para>
    ///
    /// <para>Die Zählung selbst läuft ohne Datenbank; <see cref="SzenarioAbdeckung.Lesen"/>
    /// wird am BHKW-Projekt 1030 geprüft (Strom 60, Erdgas E 63 — dieselben Träger wie in
    /// <see cref="SzenarioParameterTests"/>).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class SzenarioAbdeckungTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        private const int PROJEKT_BHKW = 1030;
        private const int TRAEGER_STROM = 60;       // „Elektrische Energie", 0,25 €/kWh, 2.400 €/a
        private const int TRAEGER_ERDGAS = 63;      // „Erdgas E", 0,84 €/Nm³, 1.200 €/a

        private const string BEST = WirtschaftlichkeitSzenario.BEST;
        private const string WORST = WirtschaftlichkeitSzenario.WORST;

        private static readonly CultureInfo DE = CultureInfo.GetCultureInfo("de-DE");
        private static readonly CultureInfo EN = CultureInfo.GetCultureInfo("en-US");

        /// <summary>Die Grundmenge ohne Träger und ohne PV: 7 + Zeitraum, Menge, EV PV, EV KWK.</summary>
        private const int GRUNDMENGE = 11;

        private static WirtschaftlichkeitParameter Satz() => new WirtschaftlichkeitParameter
        {
            IdStamm = PROJEKT_BHKW,
            Zinssatz = 3.5,
            Betrachtungszeitraum = 20,
            PreissteigerungEnergie = 2.5,
            PreissteigerungBetrieb = 1.5,
            Einspeiseverguetung = 0.082,
            EinspeiseverguetungKWK = null,
            SatzBest = SzenarioSatz.Vorgabe(BEST),
            SatzWorst = SzenarioSatz.Vorgabe(WORST)
        };

        private static SzenarioAbdeckungTraeger Strom() => new SzenarioAbdeckungTraeger
        {
            Name = "Elektrische Energie", IstStrom = true, Arbeitspreis = 0.25, Grundpreis = 2400
        };

        private static SzenarioAbdeckungTraeger Gas() => new SzenarioAbdeckungTraeger
        {
            Name = "Erdgas E", Arbeitspreis = 0.84, Grundpreis = 1200
        };

        private static SzenarioAbdeckung Zaehle(WirtschaftlichkeitParameter p,
                                                params SzenarioAbdeckungTraeger[] traeger)
            => SzenarioAbdeckung.Zaehle(p, traeger, null);

        // =================================================================
        //  m — die Parameter
        // =================================================================

        [Fact]
        public void Ohne_Traeger_und_PV_zaehlt_die_Grundmenge_elf_Parameter()
        {
            SzenarioAbdeckung a = SzenarioAbdeckung.Zaehle(Satz(), null, null);

            Assert.Equal(GRUNDMENGE, a.Parameter);
            Assert.Equal(0, a.Szenariert);
            Assert.Empty(a.Gepflegte);
            Assert.False(a.Leer);
            Assert.Equal("0 von 11 Parametern szenariert", a.Satz(DE));
        }

        /// <summary>Ohne Parametersatz gibt es keinen Ausweis — leer, nicht „0 von 0".</summary>
        [Fact]
        public void Ohne_Parametersatz_gibt_es_keinen_Ausweis()
        {
            SzenarioAbdeckung a = SzenarioAbdeckung.Zaehle(null, new[] { Gas() }, null);

            Assert.True(a.Leer);
            Assert.Equal(0, a.Parameter);
            Assert.Equal("", a.Satz(DE));
            Assert.True(SzenarioAbdeckung.Lesen(null, null).Leer);
        }

        /// <summary>
        /// Je Träger Arbeits- und Grundpreis; der Leistungspreis beim Stromträger immer, sonst
        /// nur, wo einer gepflegt ist — Erwartet oder je Szenario. Je Vergütungszeile eines
        /// PV-Standes DV-Entgelt und PPA-Preis.
        /// </summary>
        [Fact]
        public void Traeger_und_PV_Zeilen_bringen_ihre_Parameter_mit()
        {
            Assert.Equal(GRUNDMENGE + 3, Zaehle(Satz(), Strom()).Parameter);
            Assert.Equal(GRUNDMENGE + 2, Zaehle(Satz(), Gas()).Parameter);

            SzenarioAbdeckungTraeger mitLeistung = Gas();
            mitLeistung.Leistungspreis = 12;
            Assert.Equal(GRUNDMENGE + 3, Zaehle(Satz(), mitLeistung).Parameter);

            // Ein gepflegter Szenario-Leistungspreis bringt seinen Parameter mit: n ≤ m.
            SzenarioAbdeckungTraeger nurSzenario = Gas();
            nurSzenario.Szenario.LeistungspreisBest = 15;
            SzenarioAbdeckung a = Zaehle(Satz(), nurSzenario);
            Assert.Equal(GRUNDMENGE + 3, a.Parameter);
            Assert.Equal(1, a.Szenariert);
            Assert.Equal(new[] { "Leistungspreis Erdgas E" }, a.Gepflegte);

            SzenarioAbdeckung pv = SzenarioAbdeckung.Zaehle(Satz(), new[] { Strom(), Gas() },
                new[] { new SzenarioAbdeckungPv { Modell = new ProjektPhotovoltaikModel() } });
            Assert.Equal(GRUNDMENGE + 3 + 2 + 2, pv.Parameter);
            Assert.Equal(0, pv.Szenariert);

            // Eine Vergütungszeile ohne Modell zählt ihre zwei Parameter — ungepflegt.
            SzenarioAbdeckung ohneModell = SzenarioAbdeckung.Zaehle(Satz(), null,
                new[] { new SzenarioAbdeckungPv(), null });
            Assert.Equal(GRUNDMENGE + 2, ohneModell.Parameter);
            Assert.Equal(0, ohneModell.Szenariert);
        }

        // =================================================================
        //  n — die sieben Größen des W5-B-9-Satzes
        // =================================================================

        /// <summary>
        /// Die Vorgabe (leeres Feld) ist keine Pflege — auch wenn Günstig und Ungünstig mit
        /// ihr anders rechnen als Erwartet. Ein eingetragener Wert zählt, wenn er vom
        /// Erwartet-Wert abweicht; ein Wert gleich dem Erwartet-Wert zählt nicht.
        /// </summary>
        [Fact]
        public void Die_Vorgabe_zaehlt_nicht_eine_gepflegte_Abweichung_zaehlt()
        {
            WirtschaftlichkeitParameter p = Satz();
            Assert.Equal(0, Zaehle(p).Szenariert);

            p.SatzBest.Zinssatz = 3.5;                                   // = Erwartet
            p.SatzBest.PreissteigerungInvestition = 1.5;                 // = p_I wirksam (wie p_B)
            p.SatzBest.InvestitionAenderung = 0;                         // = Erwartet 0 %
            Assert.Equal(0, Zaehle(p).Szenariert);

            p.SatzWorst.Zinssatz = 4.5;
            p.SatzBest.PreissteigerungEnergie = 0;                       // 0 %/a ist eine Aussage
            p.SatzWorst.NutzungsdauerAenderung = -2;
            SzenarioAbdeckung a = Zaehle(p);

            Assert.Equal(3, a.Szenariert);
            Assert.Equal(new[] { R.WPAR_SZ_ZINS, R.WPAR_SZ_PREIS_E, R.WPAR_SZ_DAUER }, a.Gepflegte);
            Assert.Equal(GRUNDMENGE, a.Parameter);
        }

        /// <summary>
        /// p_I zählt gegen das WIRKSAME p_I des Erwartungsfalls: Ohne eigene Pflege ist das
        /// p_B — ein Szenariowert gleich p_B ist dann keine Abweichung.
        /// </summary>
        [Fact]
        public void Die_Preissteigerung_Investition_zaehlt_gegen_ihren_wirksamen_Wert()
        {
            WirtschaftlichkeitParameter p = Satz();                      // p_B = 1,5, p_I leer
            p.SatzWorst.PreissteigerungInvestition = 1.5;
            Assert.Equal(0, Zaehle(p).Szenariert);

            p.PreissteigerungInvestition = 3.0;                          // p_I gepflegt
            Assert.Equal(new[] { R.WPAR_SZ_PREIS_I }, Zaehle(p).Gepflegte);
        }

        // =================================================================
        //  n — die neuen Größen (Nullregel des Kerns)
        // =================================================================

        [Fact]
        public void Zeitraum_Menge_und_Einspeiseverguetungen_folgen_der_Nullregel()
        {
            WirtschaftlichkeitParameter p = Satz();                      // T = 20, v_pv = 0,082
            p.SatzBest.Zeitraum = 20;                                    // = T
            p.SatzBest.Menge = 0;                                        // 0 = wie Erwartet
            p.SatzBest.Einspeiseverguetung = 0.082;                      // = Erwartet
            p.SatzWorst.Einspeiseverguetung = 0;                         // 0 = wie Erwartet
            Assert.Equal(0, Zaehle(p).Szenariert);

            p.SatzWorst.Zeitraum = 25;
            p.SatzBest.Menge = 5;
            p.SatzBest.Einspeiseverguetung = 0.10;
            p.SatzWorst.EinspeiseverguetungKwk = 0.07;                   // Erwartet KWK: nicht gepflegt
            SzenarioAbdeckung a = Zaehle(p);

            Assert.Equal(4, a.Szenariert);
            Assert.Equal(new[] { R.WIRT_ANN_ZEITRAUM, R.WIRT_ANN_MENGE, R.WIRT_ANN_VERGUETUNG,
                                 R.WIRT_ANN_VERGUETUNG_KWK }, a.Gepflegte);
        }

        /// <summary>DV-Entgelt und PPA-Preis je Vergütungszeile — bei mehreren Ständen mit dem Stand.</summary>
        [Fact]
        public void DV_Entgelt_und_PPA_Preis_zaehlen_je_Verguetungszeile()
        {
            var modell = new ProjektPhotovoltaikModel
            {
                DvEntgelt = 0.4, DvEntgeltBest = 0.3,
                PpaPreis = 8.0, PpaPreisWorst = 8.0                     // = Erwartet: keine Pflege
            };
            SzenarioAbdeckung a = SzenarioAbdeckung.Zaehle(Satz(), null, new[]
            {
                new SzenarioAbdeckungPv { Name = "Variante 1", Modell = modell }
            });

            Assert.Equal(GRUNDMENGE + 2, a.Parameter);
            Assert.Equal(new[] { R.WIRT_ANN_DV_ENTGELT + " (Variante 1)" }, a.Gepflegte);
        }

        /// <summary>
        /// Die Trägerpreise zählen nach der EINEN Regel des Kerns
        /// (<see cref="TraegerpreisSzenario.Wirksam"/>): ein Szenariopreis gleich dem
        /// Erwartet-Preis oder 0 ersetzt nichts; ohne Erwartet-Preis zählt jeder gepflegte.
        /// </summary>
        [Fact]
        public void Traegerpreise_zaehlen_nach_der_einen_Regel()
        {
            SzenarioAbdeckungTraeger gas = Gas();
            gas.Szenario.ArbeitspreisBest = 0.84;                        // = Erwartet
            gas.Szenario.GrundpreisWorst = 0;                            // 0 = wie Erwartet
            SzenarioAbdeckungTraeger strom = Strom();
            Assert.Equal(0, Zaehle(Satz(), strom, gas).Szenariert);

            gas.Szenario.ArbeitspreisWorst = 0.95;
            strom.Szenario.LeistungspreisBest = 90;                      // Erwartet-Leistungspreis: keiner
            SzenarioAbdeckung a = Zaehle(Satz(), strom, gas);

            Assert.Equal(GRUNDMENGE + 3 + 2, a.Parameter);
            Assert.Equal(new[] { "Leistungspreis Elektrische Energie", "Arbeitspreis Erdgas E" }, a.Gepflegte);
        }

        // =================================================================
        //  Der Satz des Ausweises
        // =================================================================

        [Fact]
        public void Der_Satz_nennt_die_Zaehlung_und_die_gepflegten_Groessen_in_beiden_Sprachen()
        {
            WirtschaftlichkeitParameter p = Satz();
            p.SatzBest.Zeitraum = 25;
            p.SatzWorst.Menge = -10;
            SzenarioAbdeckungTraeger gas = Gas();
            gas.Szenario.ArbeitspreisBest = 0.74;
            SzenarioAbdeckung a = Zaehle(p, Strom(), gas);

            Assert.Equal(3, a.Szenariert);
            Assert.Equal(16, a.Parameter);
            Assert.Equal("3 von 16 Parametern szenariert: Betrachtungszeitraum, Mengenänderung, "
                         + "Arbeitspreis Erdgas E", a.Satz(DE));

            // Die Namen stehen in der Sprache der Zählung; der Satz selbst in der gefragten.
            using (new Kulturvorrichtung("en-US"))
            {
                SzenarioAbdeckung en = Zaehle(p, Strom(), gas);
                Assert.Equal("3 of 16 parameters with scenario values: Review period, "
                             + "Change in quantities, energy price Erdgas E", en.Satz(EN));
            }
        }

        // =================================================================
        //  Punkt 9 der Anhang-E-Checkliste
        // =================================================================

        /// <summary>
        /// Punkt 9 (Szenarioanalyse) nennt den Ausweis in seinem Stand, wo die Lage ihn kennt —
        /// sonst der allgemeine Satz; ohne Lauf bleibt er offen.
        /// </summary>
        [Fact]
        public void Punkt_9_der_Checkliste_nennt_den_Ausweis()
        {
            ChecklistenPunkt Neun(ChecklistenLage lage)
                => AnhangECheckliste.Punkte(lage).Single(p => p.Nummer == "9");

            ChecklistenPunkt mit = Neun(new ChecklistenLage
            {
                Gerechnet = true, SzenarienGerechnet = true,
                Szenarioabdeckung = "2 von 16 Parametern szenariert: Betrachtungszeitraum, Arbeitspreis Erdgas E"
            });
            // ETAPPE E13 (E9b‑Q5 b): beide Szenarien gerechnet heißt „erfüllt".
            Assert.Equal(ChecklistenStand.Erfuellt, mit.Stand);
            Assert.Equal("drei vollständige Läufe, je Szenario mit eigenem Parametersatz; 2 von 16 "
                         + "Parametern szenariert: Betrachtungszeitraum, Arbeitspreis Erdgas E.", mit.StandText);

            ChecklistenPunkt ohne = Neun(new ChecklistenLage { Gerechnet = true, SzenarienGerechnet = true });
            Assert.Equal(R.WIRT_AE_9_ERFUELLT, ohne.StandText);
            Assert.Equal("drei vollständige Läufe, je Szenario mit eigenem Parametersatz.", ohne.StandText);

            ChecklistenPunkt offen = Neun(new ChecklistenLage { Szenarioabdeckung = "1 von 11 Parametern szenariert" });
            Assert.Equal(ChecklistenStand.Offen, offen.Stand);
            Assert.Equal(R.WIRT_AE_9_OFFEN, offen.StandText);

            // Die Lage des Berichts liest den Ausweis aus der Bewertung.
            var bewertung = new WirtschaftlichkeitBewertung { Szenarioabdeckung = "0 von 11 Parametern szenariert" };
            Assert.Equal("0 von 11 Parametern szenariert",
                         ChecklistenLage.AusBericht(new List<WirtschaftlichkeitErgebnis>(), Satz(), bewertung)
                                        .Szenarioabdeckung);
        }

        // =================================================================
        //  Die zwei Helfer der Szenariotafel
        // =================================================================

        /// <summary>
        /// „Vorgaben" der Tafel: die neun Größen auf <c>null</c> — die Einspeisevergütungen
        /// stehen nicht in der Tafel und bleiben.
        /// </summary>
        [Fact]
        public void TafelZuruecksetzen_leert_die_neun_Groessen_und_laesst_die_Einspeiseverguetungen()
        {
            SzenarioSatz s = SzenarioSatz.Vorgabe(BEST);
            s.Zinssatz = 2; s.PreissteigerungEnergie = 1; s.PreissteigerungBetrieb = 1;
            s.PreissteigerungInvestition = 1; s.InvestitionAenderung = -5; s.ErtragAenderung = 5;
            s.NutzungsdauerAenderung = 2; s.Zeitraum = 25; s.Menge = 5;
            s.Einspeiseverguetung = 0.1; s.EinspeiseverguetungKwk = 0.07;

            s.TafelZuruecksetzen();

            Assert.Null(s.Zinssatz); Assert.Null(s.PreissteigerungEnergie); Assert.Null(s.PreissteigerungBetrieb);
            Assert.Null(s.PreissteigerungInvestition); Assert.Null(s.InvestitionAenderung);
            Assert.Null(s.ErtragAenderung); Assert.Null(s.NutzungsdauerAenderung);
            Assert.Null(s.Zeitraum); Assert.Null(s.Menge);
            Assert.Equal(0.1, s.Einspeiseverguetung);
            Assert.Equal(0.07, s.EinspeiseverguetungKwk);
            Assert.False(s.NurVorgaben);                                 // die Vergütungen sind Pflege
        }

        /// <summary>
        /// Die Nachweiszeile der Seite und des Berichts (E9a) nennt Zeitraum und Vergütung
        /// IMMER; die Herleitungszeile des Dialogs (<c>nurGepflegt</c>) nennt sie nur, wenn
        /// dieses Szenario sie gepflegt hat — ein Zeitraum gleich T ist keine Pflege. Die
        /// bisherige Fassung ist Zeichen für Zeichen die mit <c>false</c>.
        /// </summary>
        [Fact]
        public void Die_Herleitungszeile_des_Dialogs_nennt_die_neuen_Groessen_nur_wenn_gepflegt()
        {
            WirtschaftlichkeitParameter p = Satz();
            p.EinspeiseverguetungKWK = 0.09;
            SzenarioSatz s = p.SatzBest;

            string seite = s.Nachweis(p, DE);
            Assert.Equal(seite, s.Nachweis(p, DE, false));
            Assert.Contains("T = 20 a", seite);
            Assert.Contains("Einspeisevergütung 0,082 €/kWh", seite);
            Assert.Contains("Einspeisevergütung KWK 0,090 €/kWh", seite);

            string dialog = s.Nachweis(p, DE, true);
            Assert.DoesNotContain("T = ", dialog);
            Assert.DoesNotContain("Einspeisevergütung", dialog);
            Assert.DoesNotContain("Mengen", dialog);
            Assert.StartsWith("i = 2,5 % · p_E = 1,5 %/a", dialog);

            s.Zeitraum = 20;                                             // = T: keine Pflege
            Assert.DoesNotContain("T = ", s.Nachweis(p, DE, true));

            s.Zeitraum = 25;
            s.Menge = -10;
            s.Einspeiseverguetung = 0.1;
            s.EinspeiseverguetungKwk = 0.11;
            dialog = s.Nachweis(p, DE, true);
            Assert.Contains("i = 2,5 % · T = 25 a · p_E", dialog);
            Assert.Contains("Einspeisevergütung 0,100 €/kWh", dialog);
            Assert.Contains("Einspeisevergütung KWK 0,110 €/kWh", dialog);
            Assert.Contains("Mengen -10 %", dialog);
        }

        // =================================================================
        //  Die Ressourcen
        // =================================================================

        /// <summary>
        /// Jeder neue Schlüssel der Etappe steht in beiden Sprachen; der frühere Hinweistext
        /// ist aus beiden verschwunden.
        /// </summary>
        [Theory]
        [InlineData("WIRT_SZ_ABDECKUNG")]
        [InlineData("WIRT_SZ_ABDECKUNG_LISTE")]
        [InlineData("WIRT_AE_9_ABDECKUNG")]
        [InlineData("WIRT_AE_9_ERFUELLT")]
        [InlineData("WIRT_AE_9_TEILWEISE_ABDECKUNG")]
        [InlineData("WIRT_ANN_DV_ENTGELT")]
        [InlineData("WIRT_ANN_PPA_PREIS")]
        [InlineData("WPAR_SZ_ZEITRAUM")]
        [InlineData("WPAR_SZ_MENGE")]
        [InlineData("KI_DLG_WPA_SZ_ZEITRAUM_ERL")]
        [InlineData("KI_DLG_WPA_SZ_MENGE_ERL")]
        [InlineData("KI_DLG_CSE_GROESSE_NAME")]
        [InlineData("KI_DLG_CSE_GROESSE_ERL")]
        [InlineData("KI_DLG_CSE_ERWARTET_NAME")]
        [InlineData("KI_DLG_CSE_ERWARTET_ERL")]
        [InlineData("KI_DLG_CSE_EINHEIT_NAME")]
        [InlineData("KI_DLG_CSE_EINHEIT_ERL")]
        [InlineData("KI_DLG_CSE_NUR_KOSTEN")]
        [InlineData("SZP_TITEL")]
        [InlineData("SZP_LABEL_BEST")]
        [InlineData("SZP_LABEL_WORST")]
        [InlineData("SZP_ABSOLUT")]
        [InlineData("SZP_PROZENT")]
        [InlineData("SZP_UMRECHNUNG")]
        [InlineData("SZP_ERWARTET")]
        [InlineData("SZP_ERWARTET_LEER")]
        [InlineData("SZP_OHNE_ERWARTET")]
        [InlineData("SZP_LEER")]
        [InlineData("SZP_KNOPF_KURZ")]
        [InlineData("SZP_KNOPF_GEPFLEGT")]
        [InlineData("SZP_WIE_ERWARTET")]
        [InlineData("SZP_GEPFLEGT")]
        [InlineData("SZP_OHNE_ERWARTET_KURZ")]
        [InlineData("SZP_PV_INAKTIV")]
        [InlineData("SZP_PV_DV_OHNE_WIRKUNG")]
        [InlineData("SZP_PV_PPA_OHNE_WIRKUNG")]
        [InlineData("ETV_SZ_TITEL")]
        [InlineData("ETV_SZ_HINWEIS")]
        [InlineData("ETV_SZ_OHNE_ERWARTET")]
        [InlineData("ETV_SZ_SPEICHERFEHLER")]
        public void Jeder_neue_Schluessel_steht_in_beiden_Sprachen(string schluessel)
        {
            string de = R.ResourceManager.GetString(schluessel, DE);
            string en = R.ResourceManager.GetString(schluessel, EN);

            Assert.False(string.IsNullOrWhiteSpace(de), schluessel + " fehlt auf Deutsch.");
            Assert.False(string.IsNullOrWhiteSpace(en), schluessel + " fehlt auf Englisch.");
            Assert.NotEqual(de, en);
        }

        [Fact]
        public void Der_Hinweistext_ist_aus_beiden_Sprachen_verschwunden()
        {
            Assert.Null(R.ResourceManager.GetString("WIRT_SZEN_HINWEIS", DE));
            Assert.Null(R.ResourceManager.GetString("WIRT_SZEN_HINWEIS", EN));
            Assert.Null(typeof(R).GetProperty("WIRT_SZEN_HINWEIS"));
        }

        // =================================================================
        //  Die Zählung aus der Datenbank (Projekt 1030)
        // =================================================================

        /// <summary>
        /// <see cref="SzenarioAbdeckung.Lesen"/> zählt die Träger MIT VERBRAUCH des Projekts
        /// samt Stromträger (Strom 3, Erdgas E 2 Parameter) und nennt einen gepflegten
        /// Trägerpreis beim Namen — ein Szenariopreis gleich dem Erwartet-Preis zählt nicht,
        /// ein doppelt genannter Stand zählt einmal. <c>m</c> bleibt bei jeder Pflege gleich.
        /// </summary>
        [Fact]
        public void Lesen_zaehlt_die_Traeger_des_Projekts_und_ihre_gepflegten_Preise()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            // Die projektweiten Größen ohne Pflege — gezählt werden hier die Träger.
            WirtschaftlichkeitParameter p = new WirtschaftlichkeitCtrl().LadeParameter(PROJEKT_BHKW);
            p.SatzBest = SzenarioSatz.Vorgabe(BEST);
            p.SatzWorst = SzenarioSatz.Vorgabe(WORST);
            var stamm = new[] { new KeyValuePair<int, string>(PROJEKT_BHKW, "Stamm") };

            SzenarioAbdeckung ohne = SzenarioAbdeckung.Lesen(p, stamm);
            int m = ohne.Parameter;
            Assert.True(m >= GRUNDMENGE + 3 + 2, "m = " + m + ": Strom- und Erdgaspreise fehlen.");
            Assert.Equal(0, ohne.Szenariert);
            Assert.Equal(string.Format(DE, R.WIRT_SZ_ABDECKUNG, 0, m), ohne.Satz(DE));

            // Ein doppelt genannter Stand zählt einmal.
            Assert.Equal(m, SzenarioAbdeckung.Lesen(p, new[] { stamm[0], stamm[0] }).Parameter);

            // Ein Szenariopreis gleich dem Erwartet-Preis (0,84 €/Nm³) ist keine Pflege.
            Assert.True(EnergietraegerPreisCtrl.SzenarioSchreiben(PROJEKT_BHKW, TRAEGER_ERDGAS,
                new TraegerpreisSzenario { ArbeitspreisWorst = 0.84 }));
            Assert.Equal(0, SzenarioAbdeckung.Lesen(p, stamm).Szenariert);

            Assert.True(EnergietraegerPreisCtrl.SzenarioSchreiben(PROJEKT_BHKW, TRAEGER_ERDGAS,
                new TraegerpreisSzenario { ArbeitspreisBest = 0.74 }));
            SzenarioAbdeckung mit = SzenarioAbdeckung.Lesen(p, stamm);
            Assert.Equal(m, mit.Parameter);
            string eintrag = Assert.Single(mit.Gepflegte);
            Assert.StartsWith(R.WIRT_SZ_TP_ARBEIT + " ", eintrag);
            Assert.Contains("Erdgas", eintrag);

            // Der Stromträger zählt seinen Leistungspreis immer — eine Pflege daran ändert m nicht.
            Assert.True(EnergietraegerPreisCtrl.SzenarioSchreiben(PROJEKT_BHKW, TRAEGER_STROM,
                new TraegerpreisSzenario { LeistungspreisWorst = 150 }));
            SzenarioAbdeckung zwei = SzenarioAbdeckung.Lesen(p, stamm);
            Assert.Equal(m, zwei.Parameter);
            Assert.Equal(2, zwei.Szenariert);
            Assert.Contains(zwei.Gepflegte, g => g.StartsWith(R.WIRT_SZ_TP_LEISTUNG + " ", StringComparison.Ordinal));

            // Die projektweiten Größen kommen aus dem Parametersatz der Gruppe.
            p.SatzBest.Zeitraum = p.Betrachtungszeitraum + 5;
            SzenarioAbdeckung drei = SzenarioAbdeckung.Lesen(p, stamm);
            Assert.Equal(3, drei.Szenariert);
            Assert.Equal(R.WIRT_ANN_ZEITRAUM, drei.Gepflegte[0]);
        }
    }
}
