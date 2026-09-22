using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ETAPPE E5 Teil b — <b>die Entscheide zu den Fragen aus Teil a</b> (Anwender
    /// 22.09.2026: Q1, Q2, Q4, Q5, Q7 nach Empfehlung, Q6 in Teil b beheben, Q3 vorerst nach
    /// Empfehlung) und die Kernteile, die die Seite in Teil b neu liest.
    ///
    /// <list type="bullet">
    ///   <item><description>Q4 — die Spanne ist der Betrag aus größtem und kleinstem
    ///   Szenariowert, die Einstufung urteilt über den schlechtesten und den besten
    ///   WERT, nicht über die Etiketten;</description></item>
    ///   <item><description>Q5 — ohne gepflegten Text lautet die Risikodeklaration
    ///   „nicht monetäre Wirkungen: keine benannt";</description></item>
    ///   <item><description>Q6 — der Berichtslauf rechnet in Sicht 2 gegen A und bucht
    ///   weiter gegen die Referenz der Gruppe;</description></item>
    ///   <item><description>Q7 — ist eine Variante die Referenz, bekommt auch der Stamm
    ///   ein Urteil (und damit eine Karte);</description></item>
    ///   <item><description>die Annahmentafel und die Nr.-31-Zeile der Seite, die neuen
    ///   Schlüssel in beiden Sprachen und die Szenarionamen (Q2).</description></item>
    /// </list>
    ///
    /// <para>Q3 (nachrichtlich nur an Amortisation und Zinsfuß) prüft
    /// <see cref="ErgebnisansichtTests"/>, dort, wo die Regel seit Teil a steht.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class ErgebnisansichtEntscheideTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static readonly CultureInfo DE = CultureInfo.GetCultureInfo("de-DE");
        private static readonly CultureInfo EN = CultureInfo.GetCultureInfo("en-US");

        // =====================================================================
        //  Q4 — Spanne und Einstufung über Werte, nicht über Etiketten
        // =====================================================================

        /// <summary>
        /// Die Spanne ist der Betrag zwischen dem größten und dem kleinsten der drei
        /// Szenariowerte — auch dann, wenn das Etikett „ungünstig" die Differenz hebt oder
        /// Erwartet außerhalb von Worst und Best liegt. Ohne Worst oder Best keine Spanne.
        /// </summary>
        [Theory]
        [InlineData(100.0, 500.0, 900.0, 800.0)]
        [InlineData(900.0, 500.0, -50.0, 950.0)]        // Etiketten quer zur Wirkung
        [InlineData(500.0, 900.0, 100.0, 800.0)]        // Erwartet außerhalb von Worst/Best
        [InlineData(-300.0, -100.0, -200.0, 200.0)]
        [InlineData(double.NaN, 500.0, 900.0, double.NaN)]
        public void Q4_Die_Spanne_ist_der_Betrag_aus_groesstem_und_kleinstem_Wert(
            double worst, double erwartet, double best, double spanne)
        {
            var z = new BandbreitenZeile
            {
                Worst = double.IsNaN(worst) ? (double?)null : worst,
                Erwartet = erwartet,
                Best = double.IsNaN(best) ? (double?)null : best
            };
            if (double.IsNaN(spanne)) Assert.Null(z.Spanne);
            else Assert.Equal(spanne, z.Spanne.Value, 9);
        }

        /// <summary>
        /// Die Einstufung urteilt über den schlechtesten und den besten WERT: Hebt der
        /// Satz „ungünstig" die Differenz (Worst +9.000) und senkt der Satz „günstig" sie
        /// unter null (Best −50), ist die Variante bedingt empfohlen — und der Satz nennt
        /// −50 € als schlechtesten und +9.000 € als besten Wert, nicht die Etiketten.
        /// </summary>
        [Fact]
        public void Q4_Die_Einstufung_urteilt_ueber_den_schlechtesten_Wert()
        {
            var alle = new List<WirtschaftlichkeitErgebnis>
            {
                Ergebnis(-1, WirtschaftlichkeitSzenario.ERWARTET, null, true),
                Ergebnis(-2, WirtschaftlichkeitSzenario.ERWARTET, 5000.0, false),
                Ergebnis(-2, WirtschaftlichkeitSzenario.WORST, 9000.0, false),
                Ergebnis(-2, WirtschaftlichkeitSzenario.BEST, -50.0, false)
            };

            VariantenEmpfehlung u = Assert.Single(WirtschaftlichkeitEmpfehlung.Einstufungen(alle));
            Assert.Equal(EmpfehlungStufe.Bedingt, u.Stufe);
            Assert.Equal(-50.0, u.Schlechtester.Value, 9);
            Assert.Equal(9000.0, u.Bester.Value, 9);

            string satz = WirtschaftlichkeitEmpfehlung.Vorschlagstext(alle, DE, "Stamm");
            string zusatz = string.Format(DE, R.WIRT_EMPF_NUR_ERWARTET,
                                          WirtschaftlichkeitEmpfehlung.Geld(-50.0, DE),
                                          WirtschaftlichkeitEmpfehlung.Geld(9000.0, DE));
            Assert.Contains(zusatz, satz);

            // Die Bandbreite trägt dieselbe Stufe und die Spanne als Betrag.
            WirtschaftlichkeitBandbreite b = WirtschaftlichkeitBandbreite.Bilde(
                new[] { new KeyValuePair<int, string>(-1, "Stamm"),
                        new KeyValuePair<int, string>(-2, "Variante") }, alle, -1, "Stamm");
            BandbreitenZeile z = Assert.Single(b.Zeilen);
            Assert.Equal(EmpfehlungStufe.Bedingt, z.Urteil.Stufe);
            Assert.Equal(9050.0, z.Spanne.Value, 9);
        }

        /// <summary>
        /// Ohne Bandbreite gibt es weder den schlechtesten noch den besten Wert — die
        /// Regel urteilt dann allein nach Erwartet (Bestand).
        /// </summary>
        [Fact]
        public void Q4_Ohne_Bandbreite_gibt_es_keinen_schlechtesten_Wert()
        {
            var v = new VariantenEmpfehlung { DiffErwartet = 5000.0, DiffWorst = -100.0 };
            Assert.Null(v.Schlechtester);
            Assert.Null(v.Bester);
        }

        // =====================================================================
        //  Q5 — die Risikodeklaration ohne gepflegten Text
        // =====================================================================

        /// <summary>
        /// „nicht monetäre Wirkungen benannt" steht nur mit gepflegtem Text; ohne Text —
        /// auch bei bloßem Leerraum — lautet die Zeile „… keine benannt". Die übrigen drei
        /// Deklarationen bleiben, wie sie sind.
        /// </summary>
        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("   ", false)]
        [InlineData("Versorgungssicherheit", true)]
        public void Q5_Ohne_gepflegten_Text_lautet_die_Risikozeile_keine_benannt(string text, bool benannt)
        {
            IReadOnlyList<ValeriDeklaration> liste = ValeriAusweis.Deklarationen(text);

            Assert.Equal(4, liste.Count);
            Assert.Equal(ValeriDeklaration.RISIKO, liste[3].Schluessel);
            Assert.Equal(benannt ? R.WIRT_DEKL_RISIKO : R.WIRT_DEKL_RISIKO_OHNE_NM, liste[3].Text);
            Assert.Equal(R.WIRT_DEKL_NOMINAL, liste[0].Text);
            if (!benannt) Assert.Contains("keine benannt", liste[3].Text);
        }

        // =====================================================================
        //  Q7 — ist eine Variante die Referenz, bekommt auch der Stamm ein Urteil
        // =====================================================================

        /// <summary>
        /// Referenz ist Variante 1 (−2): Der Stamm (−1) trägt eine Differenz gegen sie und
        /// bekommt deshalb ein Urteil und eine Zeile der Bandbreite — die Referenz keines.
        /// Ohne Referenz (Bestand) bleibt der Stamm ohne Urteil.
        /// </summary>
        [Fact]
        public void Q7_Ist_eine_Variante_die_Referenz_bekommt_der_Stamm_ein_Urteil()
        {
            var alle = new List<WirtschaftlichkeitErgebnis>();
            foreach (string sz in WirtschaftlichkeitSzenario.Alle)
            {
                alle.Add(Ergebnis(-1, sz, -300.0, true));   // Stamm gegen Variante 1
                alle.Add(Ergebnis(-2, sz, null, false));    // die Referenz
                alle.Add(Ergebnis(-3, sz, 800.0, false));   // Variante 2 gegen Variante 1
            }

            List<VariantenEmpfehlung> mitReferenz = WirtschaftlichkeitEmpfehlung.Einstufungen(alle, -2);
            Assert.Equal(new[] { -1, -3 }, mitReferenz.Select(u => u.IdProjekt).ToArray());
            Assert.Equal(EmpfehlungStufe.Nicht, mitReferenz[0].Stufe);
            Assert.Equal(EmpfehlungStufe.Empfohlen, mitReferenz[1].Stufe);

            // Bestand: ohne Referenz urteilt die Regel nicht über den Stamm.
            Assert.Equal(new[] { -3 },
                         WirtschaftlichkeitEmpfehlung.Einstufungen(alle).Select(u => u.IdProjekt).ToArray());

            WirtschaftlichkeitBandbreite b = WirtschaftlichkeitBandbreite.Bilde(
                new[]
                {
                    new KeyValuePair<int, string>(-1, "Stamm"),
                    new KeyValuePair<int, string>(-2, "Variante 1"),
                    new KeyValuePair<int, string>(-3, "Variante 2")
                }, alle, -2, "Variante 1");
            Assert.Equal(new[] { -1, -3 }, b.Zeilen.Select(z => z.IdProjekt).ToArray());
            Assert.True(b.Zeile(-1).IstStamm);
            Assert.NotNull(b.Zeile(-1).Urteil);
            Assert.Equal(EmpfehlungStufe.Nicht, b.Zeile(-1).Urteil.Stufe);
            Assert.Null(b.Zeile(-2));
        }

        // =====================================================================
        //  Q6 — der Berichtslauf rechnet in Sicht 2 gegen A und bucht gegen die Gruppe
        // =====================================================================

        /// <summary>
        /// <b>Der Befund:</b> Die Berichtshülle setzte die Sicht erst NACH dem Sammeln —
        /// gerechnet war gegen die Referenz der Gruppe, die Tafeln nannten A. <b>Jetzt</b>
        /// steht die Sicht vor dem Rechenschritt am Baum: Die Zahlen des Berichts rechnen
        /// gegen A (B hat eine Differenz B − A, A keine), Bewertung und Sensitivität nennen
        /// A; der GEBUCHTE Stand rechnet weiter gegen die Referenz der Gruppe.
        ///
        /// <para>Prüfgruppe 1040–1042 (gebuchter Stand in der Testdatenbank), A = 1041,
        /// B = 1042.</para>
        /// </summary>
        [Fact]
        public void Q6_Der_Bericht_rechnet_in_Sicht_2_gegen_A_und_bucht_gegen_die_Gruppe()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            BerichtsDaten daten = Gruppe1040();
            daten.Sicht = new Vergleichssicht { Sicht = Vergleichssicht.PAAR, IdA = 1041, IdB = 1042 };

            new BerichtsDatenSammler().RechneWirtschaftlichkeit(daten, null, CancellationToken.None);

            // Die Zahlen des Berichts: gegen A.
            WirtschaftlichkeitErgebnis a = Erwartet(daten.Wirtschaftlichkeit, 1041);
            WirtschaftlichkeitErgebnis bErg = Erwartet(daten.Wirtschaftlichkeit, 1042);
            Assert.Null(a.KapitalwertDiff);
            Assert.True(bErg.KapitalwertDiff.HasValue);
            Assert.Equal(bErg.Kapitalwert.Value - a.Kapitalwert.Value, bErg.KapitalwertDiff.Value, 2);

            // Die Bewertung nennt dieselbe Referenz — und der Stamm bekommt ein Urteil (Q7).
            Assert.NotNull(daten.Bewertung);
            Assert.Equal(1041, daten.Bewertung.Bandbreite.IdReferenz);
            Assert.Equal(new[] { 1040, 1042 },
                         daten.Bewertung.Bandbreite.Zeilen.Select(z => z.IdProjekt).ToArray());
            Assert.NotNull(daten.Bewertung.Bandbreite.Zeile(1040).Urteil);
            Assert.DoesNotContain(daten.Bewertung.Sensitivitaet, z => z.IdProjekt == 1041);
            Assert.Contains(daten.Bewertung.Sensitivitaet, z => z.IdProjekt == 1042);

            // Der gebuchte Stand: gegen die Referenz der Gruppe, nicht gegen A.
            int idGruppe = Gruppenreferenz(daten);
            Assert.NotEqual(1041, idGruppe);
            List<WirtschaftlichkeitErgebnis> gebucht =
                new WirtschaftlichkeitCtrl().LadeErgebnisse(new List<int> { 1040, 1041, 1042 });
            Assert.Equal(9, gebucht.Count);
            WirtschaftlichkeitErgebnis refGebucht = Erwartet(gebucht, idGruppe);
            Assert.Null(refGebucht.KapitalwertDiff);
            foreach (int id in new[] { 1040, 1041, 1042 })
            {
                if (id == idGruppe) continue;
                WirtschaftlichkeitErgebnis e = Erwartet(gebucht, id);
                Assert.True(e.KapitalwertDiff.HasValue, id + " ohne Differenz gebucht.");
                double erwartet = e.Kapitalwert.Value - refGebucht.Kapitalwert.Value;
                Assert.True(Math.Abs(erwartet - e.KapitalwertDiff.Value) <= 0.02,
                            id + ": gebuchte Differenz " + e.KapitalwertDiff.Value.ToString("N2", DE) +
                            " ist nicht die gegen die Gruppenreferenz (" + erwartet.ToString("N2", DE) + ").");
            }
        }

        /// <summary>
        /// GEGENPROBE zu Q6: In Sicht 1 ist der Bericht der gebuchte Lauf selbst — die
        /// Bewertung nennt die Referenz der Gruppe, und A hat eine Differenz.
        /// </summary>
        [Fact]
        public void Q6_In_Sicht_1_ist_der_Bericht_der_gebuchte_Lauf()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            BerichtsDaten daten = Gruppe1040();
            daten.Sicht = null;
            new BerichtsDatenSammler().RechneWirtschaftlichkeit(daten, null, CancellationToken.None);

            int idGruppe = Gruppenreferenz(daten);
            Assert.NotNull(daten.Bewertung);
            Assert.Equal(idGruppe, daten.Bewertung.Bandbreite.IdReferenz);
            Assert.Null(Erwartet(daten.Wirtschaftlichkeit, idGruppe).KapitalwertDiff);
        }

        // =====================================================================
        //  Die Annahmentafel und die Nr.-31-Zeile der Seite
        // =====================================================================

        /// <summary>
        /// Die Annahmentafel nennt je Größe den WIRKSAMEN Wert in Ungünstig, Erwartet und
        /// Günstig — ohne Pflege die Vorgaben (Zins ∓ 1 %-Punkt, Preissteigerungen ∓ 1,
        /// Investition ± 10 %, Erträge ∓ 10 %, Nutzungsdauer ∓ 2 a) — und die Herkunft; der
        /// Betrachtungszeitraum steht in allen drei gleich. Ein gepflegter Wert heißt
        /// „gepflegt" und steht als Zahl da.
        /// </summary>
        [Fact]
        public void Die_Annahmentafel_nennt_die_wirksamen_Werte_und_ihre_Herkunft()
        {
            var p = new WirtschaftlichkeitParameter
            {
                Zinssatz = 3.0,
                Betrachtungszeitraum = 20,
                PreissteigerungEnergie = 0.0,
                PreissteigerungBetrieb = 0.0
            };

            List<AnnahmeZeile> tafel = ValeriAusweis.Annahmen(p, DE);
            Assert.Equal(new[]
                         {
                             AnnahmeZeile.ZINS, AnnahmeZeile.PREIS_E, AnnahmeZeile.PREIS_B,
                             AnnahmeZeile.PREIS_I, AnnahmeZeile.INVEST, AnnahmeZeile.ERTRAG,
                             AnnahmeZeile.DAUER, AnnahmeZeile.ZEITRAUM
                         },
                         tafel.Select(z => z.Schluessel).ToArray());

            AnnahmeZeile zins = tafel[0];
            Assert.Equal(R.WPAR_SZ_ZINS, zins.Groesse);
            Assert.Equal(new[] { "4,0 %", "3,0 %", "2,0 %" },
                         new[] { zins.Unguenstig, zins.Erwartet, zins.Guenstig });
            Assert.Equal(R.WIRT_ANN_VORGABE, zins.Herkunft);

            AnnahmeZeile energie = tafel[1];
            Assert.Equal(new[] { "+1,0 %/a", "0,0 %/a", "−1,0 %/a" },
                         new[] { energie.Unguenstig, energie.Erwartet, energie.Guenstig });

            AnnahmeZeile invest = tafel[4];
            Assert.Equal(new[] { "+10 %", "0 %", "−10 %" },
                         new[] { invest.Unguenstig, invest.Erwartet, invest.Guenstig });
            AnnahmeZeile ertrag = tafel[5];
            Assert.Equal(new[] { "−10 %", "0 %", "+10 %" },
                         new[] { ertrag.Unguenstig, ertrag.Erwartet, ertrag.Guenstig });
            AnnahmeZeile dauer = tafel[6];
            Assert.Equal(new[] { "−2 a", "0 a", "+2 a" },
                         new[] { dauer.Unguenstig, dauer.Erwartet, dauer.Guenstig });

            AnnahmeZeile zeitraum = tafel[7];
            Assert.Equal(new[] { "20 a", "20 a", "20 a" },
                         new[] { zeitraum.Unguenstig, zeitraum.Erwartet, zeitraum.Guenstig });
            Assert.Equal(R.WIRT_ANN_PROJEKTWERT, zeitraum.Herkunft);

            // Ein gepflegter Zins des ungünstigen Satzes gilt unverändert — und heißt so.
            p.SatzWorst.Zinssatz = 5.5;
            AnnahmeZeile gepflegt = ValeriAusweis.Annahmen(p, DE)[0];
            Assert.Equal("5,5 %", gepflegt.Unguenstig);
            Assert.Equal("2,0 %", gepflegt.Guenstig);
            Assert.Equal(R.WIRT_ANN_GEPFLEGT, gepflegt.Herkunft);

            Assert.Empty(ValeriAusweis.Annahmen(null, DE));
        }

        /// <summary>
        /// Die Nr.-31-Zeile nennt die Stände ohne Nachweis je einmal, dann den Satz — und
        /// fehlt, wenn jeder Stand seinen Nachweis trägt.
        /// </summary>
        [Fact]
        public void Die_Nachweiszeile_nennt_die_Staende_je_einmal()
        {
            var staende = new List<OhneNachweisStand>
            {
                new OhneNachweisStand { IdProjekt = 1019, Anzeige = "Stamm" },
                new OhneNachweisStand { IdProjekt = 1023, Anzeige = "Test1" },
                new OhneNachweisStand { IdProjekt = 1019, Anzeige = "Stamm" }
            };
            Assert.Equal("Stamm, Test1: " + R.WIRT_NACHWEIS_NAECHSTE_RECHNUNG,
                         WirtschaftlichkeitBewertung.Nachweiszeile(staende));
            Assert.Equal("", WirtschaftlichkeitBewertung.Nachweiszeile(new List<OhneNachweisStand>()));
            Assert.Equal("", WirtschaftlichkeitBewertung.Nachweiszeile(null));
        }

        /// <summary>
        /// Die Sensitivitätszeilen der Bewertung stehen in der Reihenfolge der Stände,
        /// ohne die Referenz — und jede stetige Zeile trägt ihre Steigung.
        /// </summary>
        [Fact]
        public void Die_Sensitivitaet_folgt_den_Staenden_ohne_die_Referenz()
        {
            var zeilen = new List<SensitivitaetZeile>
            {
                new SensitivitaetZeile { IdProjekt = 3, Parameter = "Zinssatz ±1 %-Pkt",
                                         KwMinus = 100.0, KwBasis = 50.0, KwPlus = 0.0,
                                         Schritt = 1.0, SchrittInProzentpunkten = true },
                new SensitivitaetZeile { IdProjekt = 1, Parameter = "Zinssatz ±1 %-Pkt",
                                         KwMinus = 10.0, KwBasis = 5.0, KwPlus = 0.0,
                                         Schritt = 1.0, SchrittInProzentpunkten = true },
                new SensitivitaetZeile { IdProjekt = 2, Parameter = "Zinssatz ±1 %-Pkt",
                                         KwMinus = 30.0, KwBasis = 20.0, KwPlus = 10.0,
                                         Schritt = 1.0, SchrittInProzentpunkten = true }
            };
            var staende = new[]
            {
                new KeyValuePair<int, string>(1, "Stamm"),
                new KeyValuePair<int, string>(2, "Variante 1"),
                new KeyValuePair<int, string>(3, "Variante 2")
            };

            List<SensitivitaetZeile> tafel =
                WirtschaftlichkeitBewertung.Sensitivitaetszeilen(staende, zeilen, 1);
            Assert.Equal(new[] { 2, 3 }, tafel.Select(z => z.IdProjekt).ToArray());
            Assert.Equal(-10.0, tafel[0].Steigung.Value, 9);
            Assert.Equal(R.WIRT_SENS_EINHEIT_PUNKT, tafel[0].SteigungEinheit);
        }

        // =====================================================================
        //  Q2 und die Schlüssel der Teil-b-Oberfläche
        // =====================================================================

        /// <summary>
        /// Q2: Die Szenarien heißen „Ungünstig", „Erwartet", „Günstig" (en „Unfavourable",
        /// „Expected", „Favourable") — nur die Ressourcentexte, kein Code.
        /// </summary>
        [Fact]
        public void Q2_Die_Szenarien_heissen_Unguenstig_Erwartet_Guenstig()
        {
            Assert.Equal("Ungünstig", R.ResourceManager.GetString("WIRT_SZEN_WORST", DE));
            Assert.Equal("Erwartet", R.ResourceManager.GetString("WIRT_SZEN_ERWARTET", DE));
            Assert.Equal("Günstig", R.ResourceManager.GetString("WIRT_SZEN_BEST", DE));
            Assert.Equal("Unfavourable", R.ResourceManager.GetString("WIRT_SZEN_WORST", EN));
            Assert.Equal("Favourable", R.ResourceManager.GetString("WIRT_SZEN_BEST", EN));
        }

        /// <summary>Jeder neue Schlüssel der Etappe E5 Teil b steht in BEIDEN Sprachen.</summary>
        [Theory]
        [InlineData("WIRT_UMSCH_KENNZAHLEN", false)]
        [InlineData("WIRT_UMSCH_VALERI", false)]
        [InlineData("WIRT_UMSCH_TITEL", false)]
        [InlineData("WIRT_ABS_LOHNT", false)]
        [InlineData("WIRT_ABS_SICHER", false)]
        [InlineData("WIRT_ABS_WORAUS", false)]
        [InlineData("WIRT_ABS_ANNAHMEN", false)]
        [InlineData("WIRT_BTN_BERICHT", false)]
        [InlineData("WIRT_LBL_EINZELHEITEN", false)]
        [InlineData("WIRT_EMPF_REGEL", false)]
        [InlineData("WIRT_EMPF_KARTE_UNTER", false)]
        [InlineData("WIRT_KZ_TAFEL_TITEL", false)]
        [InlineData("WIRT_BB_TITEL", false)]
        [InlineData("WIRT_SENS_TITEL", false)]
        [InlineData("WIRT_SENS_SP_PARAMETER", false)]
        [InlineData("WIRT_SENS_SP_MINUS", false)]
        [InlineData("WIRT_SENS_SP_BASIS", false)]
        [InlineData("WIRT_SENS_SP_PLUS", false)]
        [InlineData("WIRT_SENS_SP_STEIGUNG", false)]
        [InlineData("WIRT_SENS_SP_EINHEIT", false)]
        [InlineData("WIRT_ANN_TITEL", false)]
        [InlineData("WIRT_ANN_SP_HERKUNFT", false)]
        [InlineData("WIRT_ANN_ZEITRAUM", false)]
        [InlineData("WIRT_ANN_VORGABE", false)]
        [InlineData("WIRT_ANN_GEPFLEGT", false)]
        [InlineData("WIRT_ANN_PROJEKTWERT", false)]
        [InlineData("WIRT_DEKL_RISIKO_OHNE_NM", false)]
        [InlineData("WIRT_VALERI_BLOCK_1", false)]
        [InlineData("WIRT_VALERI_BLOCK_2", false)]
        [InlineData("WIRT_VALERI_BLOCK_3", false)]
        [InlineData("WIRT_VALERI_BLOCK_4", false)]
        [InlineData("WIRT_VALERI_BLOCK_5", false)]
        [InlineData("WIRT_VALERI_NORM_1", true)]
        [InlineData("WIRT_VALERI_NORM_2", false)]
        [InlineData("WIRT_VALERI_NORM_3", false)]
        [InlineData("WIRT_VALERI_NORM_4", true)]
        [InlineData("WIRT_VALERI_NORM_5", false)]
        [InlineData("WIRT_VALERI_BLOCK_2_HINWEIS", false)]
        [InlineData("WIRT_VALERI_MASSNAHME", false)]
        [InlineData("WIRT_VALERI_KZ_HINWEIS", false)]
        public void Der_Schluessel_steht_in_beiden_Sprachen(string schluessel, bool sprachgleich)
        {
            string de = R.ResourceManager.GetString(schluessel, DE);
            string en = R.ResourceManager.GetString(schluessel, EN);
            Assert.False(string.IsNullOrEmpty(de), schluessel + " fehlt (de).");
            Assert.False(string.IsNullOrEmpty(en), schluessel + " fehlt (en).");
            // Die Normbezüge 6.1 · 7.3 und 7.3 · 8.1.3 lauten in beiden Sprachen gleich.
            if (!sprachgleich) Assert.NotEqual(de, en);
        }

        /// <summary>Die Spanne ist im Fußtext des Berichts neu erklärt (Q4).</summary>
        [Fact]
        public void Q4_Der_Fusstext_erklaert_die_Spanne_als_groessten_minus_kleinsten_Wert()
        {
            Assert.Contains("größter minus kleinster", R.ResourceManager.GetString("WIRT_SZ_DELTA_FUSS", DE));
            Assert.Contains("largest minus smallest", R.ResourceManager.GetString("WIRT_SZ_DELTA_FUSS", EN));
        }

        // =====================================================================
        //  Hilfsmittel
        // =====================================================================

        private static WirtschaftlichkeitErgebnis Ergebnis(int id, string szenario, double? diff, bool stamm)
        {
            return new WirtschaftlichkeitErgebnis
            {
                IdProjekt = id,
                Szenario = szenario,
                IstStamm = stamm,
                Anzeige = stamm ? "Stamm" : "Variante " + (-id),
                KapitalwertDiff = diff
            };
        }

        /// <summary>Die wirksame Referenz der Gruppe — aufgelöst wie im Rechenweg
        /// (gespeicherte Gruppenreferenz, ohne Wahl oder ohne Treffer der Stamm).</summary>
        private static int Gruppenreferenz(BerichtsDaten daten)
        {
            int gewaehlt = new WirtschaftlichkeitCtrl().LadeParameter(daten.IdStamm).IdReferenzprojekt;
            return Referenzwahl.Bestimme(daten, gewaehlt).IdReferenz;
        }

        private static WirtschaftlichkeitErgebnis Erwartet(List<WirtschaftlichkeitErgebnis> alle, int id)
        {
            WirtschaftlichkeitErgebnis e = alle.FirstOrDefault(
                x => x.IdProjekt == id && x.Szenario == WirtschaftlichkeitSzenario.ERWARTET);
            Assert.NotNull(e);
            return e;
        }

        /// <summary>Die Prüfgruppe 1040–1042 (gebuchter Stand in der Testdatenbank) mit
        /// synthetischen Energiekosten — Muster <see cref="ErgebnisansichtTests"/>.</summary>
        private static BerichtsDaten Gruppe1040()
        {
            var daten = new BerichtsDaten { IdStamm = 1040, Stammprojektname = "Stammprojekt" };
            daten.Varianten.Add(Stand(1040, true, "Stammprojekt", 12000.0));
            daten.Varianten.Add(Stand(1041, false, "Variante A", 9000.0));
            daten.Varianten.Add(Stand(1042, false, "Variante B", 7000.0));
            return daten;
        }

        private static VariantenDaten Stand(int id, bool istStamm, string name, double energie)
        {
            return new VariantenDaten
            {
                IdProjekt = id,
                IstStamm = istStamm,
                Projektname = "Stammprojekt",
                Variantenname = istStamm ? "" : name,
                Ergebnis = new ErgebnisModel(),
                Energiekosten = energie
            };
        }
    }
}
