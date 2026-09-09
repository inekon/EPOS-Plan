using System;
using System.Collections.Generic;
using System.Globalization;
using WindowsFormsApplication1;
using MyResource = WindowsFormsApplication1.MyResource;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ANWENDERENTSCHEID W5‑B‑11 (09.09.2026): „VALERI-Gaps ohne Datenmodell."
    ///
    /// <para>Der VALERI-Abgleich der Etappe W5‑B‑10 hatte elf Lücken benannt (G1…G11).
    /// Der Anwender hat entschieden: <b>G11, G8, G9, G7 und G10 jetzt</b> — sie kommen
    /// ohne neue Spalte aus; <b>G4, G2 und G6 mit Migrationsschritt 72</b> (Etappe
    /// W5‑B‑12); <b>G1, G3 und G5 gar nicht</b>, aber offengelegt.</para>
    ///
    /// <para><b>Was diese Fälle festhalten:</b></para>
    /// <list type="bullet">
    ///   <item><description><b>G11</b> — die investitionsgekoppelten Betriebskosten
    ///   („x % der Investitionssumme") folgen dem Szenario-Investitionsausschlag, und
    ///   zwar nach derselben Vorrangregel wie die Investitionszeilen selbst: Eine
    ///   gepflegte Zeile bleibt Basis, wie sie dasteht. Ohne Satz bleibt alles
    ///   bitgleich.</description></item>
    ///   <item><description><b>G9</b> — die Einstufungsregel der Entscheidungsempfehlung
    ///   und die Wahl des Gesamtvorschlags.</description></item>
    ///   <item><description><b>G7</b> — der Betrachtungszeitraum gegen die
    ///   Nutzungsdauern (Restwert, Ersatz, deckungsgleich, keine Dauer).</description></item>
    /// </list>
    ///
    /// <para><b>Der Träger der Datenbankfälle</b> ist — wie in
    /// <see cref="BetriebskostenBasisTests"/> — das BHKW des Projekts 1018
    /// (Komponente 7, Anlage 11327): die einzige Anlage der Testdatenbank, die
    /// Kategorie-1-Kaskade UND eine Kategorie-2-Zeile „% der Investition" führt.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class ValeriLueckenTests
    {
        private readonly ITestOutputHelper _ausgabe;

        public ValeriLueckenTests(ITestOutputHelper ausgabe) { _ausgabe = ausgabe; }

        private static readonly CultureInfo DE = new CultureInfo("de-DE");

        // ---- Projekt 1018, BHKW (dieselben Zeilen wie BetriebskostenBasisTests) ----
        private const int PROJEKT = 1018;
        private const int BHKW = 7;
        private const int ANLAGE = 11327;

        private const int I_HAUPT = 101600101;  // BETRAG 45.312,50 €, IsMainComponent
        private const int I_SATZ = 101600546;   // 200,00 €/kW el × 14,5 kW el
        private const int I_ERZ = 101600549;    // 5 % der Erzeugerkosten
        private const int I_P10 = 101600551;    // 10 % der Investition
        private const int I_P5 = 101600553;     //  5 % der Investition
        private static readonly int[] I_NULLZEILEN =
            { 101600547, 101600548, 101600550, 101600552 };

        private const int B_P2 = 101600555;     // Kategorie 2: 2 % der Investitionssumme

        // Die Zahlen des Beispiels (Rechenweg in BetriebskostenBasisTests):
        //   45.312,50 + 2.900,00 + 2.265,625      = 50.478,125  (Basis der Runde 3)
        //   + 10 % = 5.047,8125 · + 5 % = 2.523,90625
        //   ------------------------------------------------------------------
        //   Kaskadensumme der Anlage              = 58.049,84375 €
        //   Betriebszeile 2 %                     =  1.160,996875 €/a
        private const double KASKADE_ANLAGE = 58049.84375;
        private const double BETRIEB_ERWARTET = 1160.996875;

        // =====================================================================
        // G11 — investitionsgekoppelte Betriebskosten im Szenario
        // =====================================================================

        /// <summary>
        /// OHNE Satz ist die Betriebskostenrechnung Zeichen für Zeichen die von vor
        /// dieser Etappe — der Weg des Szenarios ERWARTET und jeder Anzeige. Das ist
        /// die Zusage, an der die Regressionsprobe hängt.
        /// </summary>
        [Fact]
        public void Ohne_Satz_bleiben_die_Betriebskosten_unveraendert()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();

            WirtschaftlichkeitCtrl.BetriebsTopfe alt =
                WirtschaftlichkeitCtrl.LiesBetriebskostenTopfe(
                    PROJEKT, WirtschaftlichkeitSzenario.ERWARTET);
            WirtschaftlichkeitCtrl.BetriebsTopfe neu =
                WirtschaftlichkeitCtrl.LiesBetriebskostenTopfe(
                    PROJEKT, WirtschaftlichkeitSzenario.ERWARTET, null);

            // BITgleich, nicht nur gerundet gleich.
            Assert.Equal(alt.BetriebSofort, neu.BetriebSofort);
            Assert.Equal(alt.EndenergieSofort, neu.EndenergieSofort);
            Assert.Equal(alt.InvestGekoppeltSofort, neu.InvestGekoppeltSofort);
            Assert.Equal(alt.Gesamt, neu.Gesamt);
            Assert.Equal(BETRIEB_ERWARTET, neu.Gesamt, 6);
        }

        /// <summary>
        /// DER BEFUND G11: Im Worst-Fall kostet die Anlage 10 % mehr — dann kostet
        /// auch ihre Wartung „2 % der Investitionssumme" 10 % mehr. Bis W5‑B‑10 bemaß
        /// sie sich stumm an der Investition des ERWARTUNGSfalls.
        /// </summary>
        [Fact]
        public void Die_Prozentzeile_folgt_dem_Investitionsausschlag_des_Szenarios()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();

            SzenarioSatz worst = SzenarioSatz.Vorgabe(WirtschaftlichkeitSzenario.WORST);
            SzenarioSatz best = SzenarioSatz.Vorgabe(WirtschaftlichkeitSzenario.BEST);

            // Die BEMESSUNGSBASIS wächst mit dem Ausschlag: keine Zeile ist gepflegt,
            // also skaliert die ganze Kaskadensumme.
            Assert.Equal(KASKADE_ANLAGE * 1.1,
                BetriebskostenCtrl.InvestSummeFuer(PROJEKT, BHKW, ANLAGE,
                    BetriebskostenCtrl.Kaskadensummen(PROJEKT, worst)).Value, 6);

            // … und mit ihr die Betriebskostenzeile.
            Assert.Equal(BETRIEB_ERWARTET * 1.1,
                WirtschaftlichkeitCtrl.LiesBetriebskostenTopfe(
                    PROJEKT, WirtschaftlichkeitSzenario.WORST, worst).Gesamt, 6);
            Assert.Equal(BETRIEB_ERWARTET * 0.9,
                WirtschaftlichkeitCtrl.LiesBetriebskostenTopfe(
                    PROJEKT, WirtschaftlichkeitSzenario.BEST, best).Gesamt, 6);

            // Das Fehlerbild vor W5‑B‑11: dieselbe Zahl in allen drei Szenarien.
            Assert.Equal(BETRIEB_ERWARTET,
                WirtschaftlichkeitCtrl.LiesBetriebskostenTopfe(
                    PROJEKT, WirtschaftlichkeitSzenario.WORST).Gesamt, 6);
        }

        /// <summary>
        /// DIE VORRANGREGEL GILT AUCH FÜR DIE BASIS: Eine Investitionszeile mit
        /// gepflegtem Worst-Case-Betrag trägt genau diesen Betrag zur Bemessungsbasis
        /// bei — nicht diesen Betrag mal 1,1. Nur der nicht gepflegte Anteil skaliert.
        ///
        /// <para>Rechenweg mit gepflegten 50.000,00 € auf der Hauptposition:</para>
        /// <code>
        ///   Hauptposition (gepflegt)      50.000,00   unskaliert
        ///   200 €/kW × 14,5 kW             2.900,00 × 1,1 =  3.190,00
        ///   5 % der Erzeugerkosten         2.500,00 × 1,1 =  2.750,00
        ///   ---------------------------------------------------------
        ///   Basis der Runde 3             55.400,00
        ///   10 % der Investition           5.540,00 × 1,1 =  6.094,00
        ///    5 % der Investition           2.770,00 × 1,1 =  3.047,00
        ///   =========================================================
        ///   Bemessungsbasis               65.081,00 €   →  2 % = 1.301,62 €/a
        /// </code>
        /// <para>Der Notweg „alles mal 1,1" käme auf 70.081,00 € und 1.401,62 €/a —
        /// genau die Doppelzählung, die die Vorrangregel verhindert.</para>
        /// </summary>
        [Fact]
        public void Ein_gepflegter_Zeilenwert_bleibt_auch_in_der_Bemessungsbasis_stehen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();
            DataRepository.ExecuteSQL(
                "UPDATE Tab_ProjektWerte SET WorstCase = 50000 WHERE ID = " + I_HAUPT);

            SzenarioSatz worst = SzenarioSatz.Vorgabe(WirtschaftlichkeitSzenario.WORST);

            // Die Kaskade weiß, woher der Wert kam — daran hängt die Vorrangregel.
            Dictionary<int, InvestKaskade.Zeile> k =
                InvestKaskade.NachId(PROJEKT, WirtschaftlichkeitSzenario.WORST);
            Assert.True(k[I_HAUPT].WertGepflegt);
            Assert.False(k[I_SATZ].WertGepflegt);
            Assert.Equal(50000.0, InvestKaskade.BetragImSzenario(k[I_HAUPT], worst), 6);
            Assert.Equal(2900.0 * 1.1, InvestKaskade.BetragImSzenario(k[I_SATZ], worst), 6);

            Assert.Equal(65081.0,
                BetriebskostenCtrl.InvestSummeFuer(PROJEKT, BHKW, ANLAGE,
                    BetriebskostenCtrl.Kaskadensummen(PROJEKT, worst)).Value, 6);

            Assert.Equal(1301.62,
                WirtschaftlichkeitCtrl.LiesBetriebskostenTopfe(
                    PROJEKT, WirtschaftlichkeitSzenario.WORST, worst).Gesamt, 6);

            // Der Notweg (pauschal alles × 1,1) käme hier heraus — er tut es nicht.
            Assert.NotEqual(1401.62,
                WirtschaftlichkeitCtrl.LiesBetriebskostenTopfe(
                    PROJEKT, WirtschaftlichkeitSzenario.WORST, worst).Gesamt, 6);
        }

        /// <summary>
        /// Die Nachweisliste des Berichts (E7) MUSS die Summe der Rechnung treffen —
        /// auch im Szenario. Sonst wiese der Bericht eine andere Zahl aus als die, mit
        /// der gerechnet wurde.
        /// </summary>
        [Fact]
        public void Die_Nachweisliste_trifft_die_Summe_auch_im_Szenario()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();

            SzenarioSatz worst = SzenarioSatz.Vorgabe(WirtschaftlichkeitSzenario.WORST);
            double summe = 0;
            foreach (KostenPositionNachweis n in
                     WirtschaftlichkeitCtrl.LiesBetriebskostenPositionen(
                         PROJEKT, WirtschaftlichkeitSzenario.WORST, worst))
                summe += n.BetragJahr;

            Assert.Equal(BETRIEB_ERWARTET * 1.1,
                WirtschaftlichkeitCtrl.LiesBetriebskostenTopfe(
                    PROJEKT, WirtschaftlichkeitSzenario.WORST, worst).Gesamt, 6);
            Assert.Equal(BETRIEB_ERWARTET * 1.1, summe, 6);
        }

        /// <summary>
        /// DER REGRESSIONSBELEG: Der Kapitalwert des Worst-Falls wird durch G11
        /// UNGÜNSTIGER — die investitionsgekoppelten Betriebskosten steigen mit der
        /// Investition. Genau das war der Zweck des Entscheids; die Zahlen stehen im
        /// Protokoll.
        /// </summary>
        [Fact]
        public void Der_Kapitalwert_im_Worst_Fall_wird_durch_G11_unguenstiger()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            BeispielAnlegen();

            var p = new WirtschaftlichkeitParameter { Zinssatz = 3.0, Betrachtungszeitraum = 20 };
            SzenarioSatz worst = p.SatzFuer(WirtschaftlichkeitSzenario.WORST);
            WirtschaftlichkeitParameter ps = p.FuerSzenario(WirtschaftlichkeitSzenario.WORST);

            double zuschuss;
            List<KapitalwertRechner.InvestPosition> invest =
                WirtschaftlichkeitCtrl.LiesInvestitionen(
                    PROJEKT, WirtschaftlichkeitSzenario.WORST, worst, out zuschuss);

            double betriebVorher = WirtschaftlichkeitCtrl.LiesBetriebskostenTopfe(
                PROJEKT, WirtschaftlichkeitSzenario.WORST).Gesamt;
            double betriebNachher = WirtschaftlichkeitCtrl.LiesBetriebskostenTopfe(
                PROJEKT, WirtschaftlichkeitSzenario.WORST, worst).Gesamt;

            double kwVorher = KapitalwertRechner.Rechne(invest, betriebVorher, 3000.0, 0.0,
                ps.Zinssatz, ps.Betrachtungszeitraum,
                ps.PreissteigerungBetrieb, ps.PreissteigerungEnergie,
                0, null, zuschuss).Kapitalwert;
            double kwNachher = KapitalwertRechner.Rechne(invest, betriebNachher, 3000.0, 0.0,
                ps.Zinssatz, ps.Betrachtungszeitraum,
                ps.PreissteigerungBetrieb, ps.PreissteigerungEnergie,
                0, null, zuschuss).Kapitalwert;

            _ausgabe.WriteLine(string.Format(DE,
                "G11-Regression Projekt {0}, Worst: Betrieb {1:N6} -> {2:N6} EUR/a; " +
                "KW {3:N2} -> {4:N2} EUR (Delta {5:N2})",
                PROJEKT, betriebVorher, betriebNachher, kwVorher, kwNachher,
                kwNachher - kwVorher));

            Assert.Equal(BETRIEB_ERWARTET, betriebVorher, 6);
            Assert.Equal(BETRIEB_ERWARTET * 1.1, betriebNachher, 6);
            Assert.True(kwNachher < kwVorher,
                "Der Worst-Kapitalwert muss durch G11 sinken, nicht steigen.");
        }

        // =====================================================================
        // G9 — Entscheidungsempfehlung
        // =====================================================================

        /// <summary>Alle drei Szenarien positiv → „empfohlen", und der Satz nennt die
        /// Bandbreite.</summary>
        [Fact]
        public void Drei_positive_Szenarien_ergeben_empfohlen()
        {
            List<WirtschaftlichkeitErgebnis> alle = Gruppe(
                Variante(1031, "WP + PV", 2100.0, 12300.0, 21800.0));

            List<VariantenEmpfehlung> u = WirtschaftlichkeitEmpfehlung.Einstufungen(alle);
            Assert.Single(u);
            Assert.Equal(EmpfehlungStufe.Empfohlen, u[0].Stufe);
            Assert.False(u[0].BandbreiteFehlt);
            Assert.Equal(MyResource.Resource.WIRT_EMPF_STUFE_JA, u[0].StufeText);

            string satz = WirtschaftlichkeitEmpfehlung.Vorschlagstext(alle, DE);
            Assert.Contains("WP + PV", satz);
            Assert.Contains(WirtschaftlichkeitEmpfehlung.Geld(12300.0, DE), satz);
            Assert.Contains(WirtschaftlichkeitEmpfehlung.Geld(2100.0, DE), satz);
            Assert.Contains(WirtschaftlichkeitEmpfehlung.Geld(21800.0, DE), satz);
        }

        /// <summary>Erwartet und Best positiv, Worst negativ → „bedingt empfohlen".</summary>
        [Fact]
        public void Ein_negativer_Worst_Fall_ergibt_bedingt_empfohlen()
        {
            List<WirtschaftlichkeitErgebnis> alle = Gruppe(
                Variante(1031, "WP klein", -2100.0, 12300.0, 21800.0));

            List<VariantenEmpfehlung> u = WirtschaftlichkeitEmpfehlung.Einstufungen(alle);
            Assert.Equal(EmpfehlungStufe.Bedingt, u[0].Stufe);
            Assert.Equal(MyResource.Resource.WIRT_EMPF_STUFE_BEDINGT, u[0].StufeText);
            Assert.Contains("WP klein", WirtschaftlichkeitEmpfehlung.Vorschlagstext(alle, DE));
        }

        /// <summary>Erwartet nicht positiv → „nicht empfohlen", und es gibt keinen
        /// Vorschlag: Der Satz nennt den Weiterbetrieb.</summary>
        [Fact]
        public void Ein_nicht_positiver_Erwartungsfall_ergibt_nicht_empfohlen()
        {
            List<WirtschaftlichkeitErgebnis> alle = Gruppe(
                Variante(1031, "WP groß", -9000.0, -1200.0, 4000.0));

            List<VariantenEmpfehlung> u = WirtschaftlichkeitEmpfehlung.Einstufungen(alle);
            Assert.Equal(EmpfehlungStufe.Nicht, u[0].Stufe);
            Assert.Null(WirtschaftlichkeitEmpfehlung.Vorschlag(u));
            Assert.Equal(MyResource.Resource.WIRT_EMPF_KEINE,
                         WirtschaftlichkeitEmpfehlung.Vorschlagstext(alle, DE));
        }

        /// <summary>
        /// Fehlen Best und Worst (nicht gerechnet), urteilt die Regel allein nach
        /// Erwartet — und sagt das dazu, statt eine Bandbreite zu behaupten.
        /// </summary>
        [Fact]
        public void Ohne_Bandbreite_urteilt_nur_der_Erwartungsfall()
        {
            var alle = new List<WirtschaftlichkeitErgebnis>
            {
                Zeile(1030, "Stamm", WirtschaftlichkeitSzenario.ERWARTET, null, true),
                Zeile(1031, "Nur Erwartet", WirtschaftlichkeitSzenario.ERWARTET, 5000.0, false)
            };

            List<VariantenEmpfehlung> u = WirtschaftlichkeitEmpfehlung.Einstufungen(alle);
            Assert.Equal(EmpfehlungStufe.Empfohlen, u[0].Stufe);
            Assert.True(u[0].BandbreiteFehlt);
            Assert.Contains(MyResource.Resource.WIRT_EMPF_OHNE_BANDBREITE, u[0].StufeText);
            Assert.Contains(MyResource.Resource.WIRT_EMPF_OHNE_BANDBREITE,
                            WirtschaftlichkeitEmpfehlung.Vorschlagstext(alle, DE));
        }

        /// <summary>
        /// Der Gesamtvorschlag ist die Variante mit der HÖCHSTEN Erwartet-Differenz
        /// unter den empfohlenen — eine bedingt empfohlene mit höherer Differenz
        /// gewinnt nicht.
        /// </summary>
        [Fact]
        public void Der_Vorschlag_nimmt_die_hoechste_Differenz_der_empfohlenen()
        {
            var alle = new List<WirtschaftlichkeitErgebnis>
            {
                Zeile(1030, "Stamm", WirtschaftlichkeitSzenario.ERWARTET, null, true)
            };
            alle.AddRange(Variante(1031, "Klein", 500.0, 4000.0, 9000.0));      // empfohlen
            alle.AddRange(Variante(1032, "Mittel", 900.0, 8000.0, 15000.0));    // empfohlen, höher
            alle.AddRange(Variante(1033, "Groß", -400.0, 30000.0, 60000.0));    // nur bedingt

            VariantenEmpfehlung v = WirtschaftlichkeitEmpfehlung.Vorschlag(alle);
            Assert.NotNull(v);
            Assert.Equal(1032, v.IdProjekt);
            Assert.Equal(EmpfehlungStufe.Empfohlen, v.Stufe);

            // Ohne empfohlene Variante gewinnt die beste bedingt empfohlene.
            var nurBedingt = new List<WirtschaftlichkeitErgebnis>
            {
                Zeile(1030, "Stamm", WirtschaftlichkeitSzenario.ERWARTET, null, true)
            };
            nurBedingt.AddRange(Variante(1033, "Groß", -400.0, 30000.0, 60000.0));
            nurBedingt.AddRange(Variante(1034, "Mini", -50.0, 900.0, 1500.0));
            Assert.Equal(1033, WirtschaftlichkeitEmpfehlung.Vorschlag(nurBedingt).IdProjekt);
        }

        /// <summary>
        /// Ohne Stamm in der Vergleichsgruppe gibt es keine Kapitalwertdifferenz — und
        /// damit keinen Satz. Ein Vorschlag ohne Zahlen wäre eine Behauptung.
        /// </summary>
        [Fact]
        public void Ohne_Stamm_und_ohne_Variante_gibt_es_keinen_Satz()
        {
            var ohneStamm = new List<WirtschaftlichkeitErgebnis>
            {
                Zeile(1031, "Allein", WirtschaftlichkeitSzenario.ERWARTET, null, false)
            };
            Assert.Empty(WirtschaftlichkeitEmpfehlung.Einstufungen(ohneStamm));
            Assert.Equal("", WirtschaftlichkeitEmpfehlung.Vorschlagstext(ohneStamm, DE));

            Assert.Equal("", WirtschaftlichkeitEmpfehlung.Vorschlagstext(
                new List<WirtschaftlichkeitErgebnis>(), DE));
            Assert.Equal("", WirtschaftlichkeitEmpfehlung.Vorschlagstext(
                (IEnumerable<WirtschaftlichkeitErgebnis>)null, DE));
        }

        // =====================================================================
        // G7 — Betrachtungszeitraum gegen die Nutzungsdauern
        // =====================================================================

        /// <summary>T kürzer als die längste Nutzungsdauer → Restwert am Ende.</summary>
        [Fact]
        public void T_unter_der_Nutzungsdauer_weist_den_Restwert_aus()
        {
            string s = NutzungsdauerAbgleich.Hinweis(20, Positionen(25.0), DE);

            Assert.Contains(MyResource.Resource.WIRT_T_RESTWERT, s);
            Assert.DoesNotContain(MyResource.Resource.WIRT_T_GLEICH, s);
        }

        /// <summary>T länger als die kürzeste Nutzungsdauer → Ersatzbeschaffung, und
        /// zwar im Jahr der Nutzungsdauer (dieselbe Rundung wie im Rechenkern).</summary>
        [Fact]
        public void T_ueber_der_Nutzungsdauer_weist_die_Ersatzbeschaffung_aus()
        {
            string s = NutzungsdauerAbgleich.Hinweis(20, Positionen(15.0), DE);

            Assert.Contains(string.Format(DE, MyResource.Resource.WIRT_T_ERSATZ, 15), s);
            Assert.DoesNotContain(MyResource.Resource.WIRT_T_GLEICH, s);

            // Beides zugleich ist der Regelfall gemischter Anlagen: 15 a ersetzt, 25 a
            // trägt einen Restwert.
            string beides = NutzungsdauerAbgleich.Hinweis(20, Positionen(15.0, 25.0), DE);
            Assert.Contains(MyResource.Resource.WIRT_T_RESTWERT, beides);
            Assert.Contains(string.Format(DE, MyResource.Resource.WIRT_T_ERSATZ, 15), beides);
        }

        /// <summary>T = n → deckungsgleich: weder Ersatz noch Restwert.</summary>
        [Fact]
        public void T_gleich_der_Nutzungsdauer_ist_deckungsgleich()
        {
            string s = NutzungsdauerAbgleich.Hinweis(20, Positionen(20.0), DE);

            Assert.Contains(MyResource.Resource.WIRT_T_GLEICH, s);
            Assert.DoesNotContain(MyResource.Resource.WIRT_T_RESTWERT, s);
        }

        /// <summary>
        /// Keine gepflegte Nutzungsdauer (n &lt; 1 heißt im Rechenkern „wie T") → der
        /// eigene Satz: kein Ersatz, kein Restwert. Und ohne Betrachtungszeitraum gibt
        /// es gar keine Zeile.
        /// </summary>
        [Fact]
        public void Ohne_gepflegte_Nutzungsdauer_steht_der_eigene_Satz()
        {
            Assert.Equal(string.Format(DE, MyResource.Resource.WIRT_T_OHNE_DAUER, 20),
                         NutzungsdauerAbgleich.Hinweis(20, Positionen(0.0), DE));
            Assert.Equal(string.Format(DE, MyResource.Resource.WIRT_T_OHNE_DAUER, 20),
                         NutzungsdauerAbgleich.Hinweis(20, null, DE));
            Assert.Equal("", NutzungsdauerAbgleich.Hinweis(0, Positionen(15.0), DE));
        }

        // =====================================================================
        // Hilfsmittel
        // =====================================================================

        /// <summary>Legt das BHKW-Beispiel in der ARBEITSKOPIE an und räumt alle
        /// Szenariospalten des Projekts leer — sonst entschiede ein Bestandswert der
        /// Testdatenbank über das Ergebnis.</summary>
        private static void BeispielAnlegen()
        {
            Setze(I_HAUPT, 45312.5, DbWerte.BEMESSUNG_BETRAG, null, DbWerte.KOSTENART_KAPITALGEBUNDEN);
            foreach (int id in I_NULLZEILEN)
                Setze(id, 0.0, DbWerte.BEMESSUNG_BETRAG, null, DbWerte.KOSTENART_KAPITALGEBUNDEN);
            Setze(I_SATZ, 0.0, DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH, 200.0,
                  DbWerte.KOSTENART_KAPITALGEBUNDEN);
            Setze(I_ERZ, 0.0, DbWerte.BEMESSUNG_PROZENT_ERZEUGERKOSTEN, 5.0,
                  DbWerte.KOSTENART_KAPITALGEBUNDEN);
            Setze(I_P10, 0.0, DbWerte.BEMESSUNG_PROZENT_INVESTITION, 10.0,
                  DbWerte.KOSTENART_KAPITALGEBUNDEN);
            Setze(I_P5, 0.0, DbWerte.BEMESSUNG_PROZENT_INVESTITION, 5.0,
                  DbWerte.KOSTENART_KAPITALGEBUNDEN);
            Setze(B_P2, 0.0, DbWerte.BEMESSUNG_PROZENT_INVESTITION, 2.0,
                  DbWerte.KOSTENART_BETRIEBSGEBUNDEN);

            DataRepository.ExecuteSQL(
                "UPDATE Tab_ProjektWerte SET BestCase = 0, WorstCase = 0, " +
                "BestCase_Nutzungsdauer = 0, WorstCase_Nutzungsdauer = 0 " +
                "WHERE ProjektID = " + PROJEKT);
        }

        private static void Setze(int id, double wert, string bemessung, double? satz, string kostenart)
        {
            var p = new DbParam("@e", DbParamTyp.Double);
            p.Wert = satz.HasValue ? (object)satz.Value : DBNull.Value;
            DataRepository.ExecuteSQL(
                "UPDATE Tab_ProjektWerte SET EingegebenerWert = ?, Bemessung = ?, " +
                "Einheitpreis = ?, Menge = NULL, Kostenart = ? WHERE ID = " + id,
                new DbParam("@w", wert),
                new DbParam("@b", bemessung),
                p,
                new DbParam("@k", kostenart));
        }

        /// <summary>Investitionspositionen mit den gegebenen Nutzungsdauern (Startjahr t0).</summary>
        private static List<KapitalwertRechner.InvestPosition> Positionen(params double[] dauern)
        {
            var liste = new List<KapitalwertRechner.InvestPosition>();
            foreach (double d in dauern)
                liste.Add(new KapitalwertRechner.InvestPosition { Betrag = 10000.0, Nutzungsdauer = d });
            return liste;
        }

        /// <summary>Eine Ergebniszeile eines Projekts in einem Szenario.</summary>
        private static WirtschaftlichkeitErgebnis Zeile(int id, string anzeige, string szenario,
                                                        double? diff, bool istStamm)
        {
            return new WirtschaftlichkeitErgebnis
            {
                IdProjekt = id,
                Anzeige = anzeige,
                Szenario = szenario,
                IstStamm = istStamm,
                Kapitalwert = 0.0,
                KapitalwertDiff = diff
            };
        }

        /// <summary>Die drei Szenariozeilen EINER Variante.</summary>
        private static List<WirtschaftlichkeitErgebnis> Variante(int id, string anzeige,
                                                                 double worst, double erwartet, double best)
        {
            return new List<WirtschaftlichkeitErgebnis>
            {
                Zeile(id, anzeige, WirtschaftlichkeitSzenario.WORST, worst, false),
                Zeile(id, anzeige, WirtschaftlichkeitSzenario.ERWARTET, erwartet, false),
                Zeile(id, anzeige, WirtschaftlichkeitSzenario.BEST, best, false)
            };
        }

        /// <summary>Stammzeile plus die Zeilen einer Variante.</summary>
        private static List<WirtschaftlichkeitErgebnis> Gruppe(List<WirtschaftlichkeitErgebnis> variante)
        {
            var alle = new List<WirtschaftlichkeitErgebnis>
            {
                Zeile(1030, "Stamm", WirtschaftlichkeitSzenario.ERWARTET, null, true)
            };
            alle.AddRange(variante);
            return alle;
        }
    }
}
