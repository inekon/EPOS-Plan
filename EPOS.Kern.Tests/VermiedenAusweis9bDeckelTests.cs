using System;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Deckel an der § 9b-Korrektur des Ausweises der vermiedenen Stromkosten</b>
    /// (Anwenderentscheid 02.10.2026, Register EZ‑23 „Deckel", Konzept § 2.6, § 3.6): derselbe Deckel
    /// wie im Abzug der Wärmegestehung (EZ‑22) — der wirksame § 9b-Satz ist höchstens der
    /// Stromsteueranteil des Preises, mit dem der Eigenstrom bewertet wird:
    /// <code>
    /// Korrektur = max(0, (N + M) × s_eff − S) − max(0, N × s_eff − S)
    /// s_eff     = min(s, a)       a = Stromsteueranteil des Trägers der Bezugsrolle (= Netzträger),
    ///                                 ohne gepflegten Anteil der Regelsatz des Jahres,
    ///                                 abgeschaltete Komponente 0
    /// </code>
    /// Gerechnet an 1030 mit den flachen Stundenreihen des Prüffalls B6
    /// (<see cref="StromsteuerBefreiungModusTests.Stundenreihen"/>), einer aktiven Tarifstruktur im
    /// Rollenmodell und produzierendem Gewerbe: vermiedene Menge = BHKW-Eigenverbrauch 432,3 MWh, der
    /// Netzbezug trägt den Sockel (die Korrektur ist Zeichen für Zeichen M × s_eff).
    /// </summary>
    [Collection("Testdatenbank")]
    public class VermiedenAusweis9bDeckelTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private const int PROJEKT = 1030;
        /// <summary>Der Netzträger von 1030 (im Projekt ohne gepflegten Stromsteueranteil).</summary>
        private const int NETZTRAEGER = 60;
        private const double SATZ_9B = 20.0;          // €/MWh, Katalog 2026
        private const double MENGE_MWH = 432.3;       // BHKW-Eigenverbrauch der Stundenreihen

        // =====================================================================
        //  Die Regel an der Kernfunktion (ohne Datenbank)
        // =====================================================================

        /// <summary>
        /// Der Ausweis-Anker des Beispielprojekts (Rechenweg 07) bleibt: 1.179,7 MWh × 20,00 =
        /// 23.594,0 €/a. Der Netzträger führt dort 2,05 ct/kWh = 20,50 €/MWh Stromsteuer — der Deckel
        /// liegt über dem Satz und lässt ihn unberührt, Zeichen für Zeichen. Mit 1,00 ct/kWh halbiert
        /// sich die Korrektur auf 11.797,0 €/a; mit abgeschalteter Komponente (Anteil 0) entfällt sie.
        /// </summary>
        [Fact]
        public void Der_Anker_23594_bleibt_und_der_Deckel_halbiert_bei_einem_Anteil_von_1_ct()
        {
            double ohne = SteuerGutschriftRechner.Entgangene9bEur(250.0, 1179.7, SATZ_9B, 250.0);
            double anker = SteuerGutschriftRechner.Entgangene9bEur(250.0, 1179.7, SATZ_9B, 250.0, 20.50);
            Assert.Equal(ohne, anker);
            Assert.Equal(23594.0, anker, 2);

            Assert.Equal(11797.0, SteuerGutschriftRechner.Entgangene9bEur(250.0, 1179.7, SATZ_9B, 250.0, 10.0), 2);
            Assert.Equal(0.0, SteuerGutschriftRechner.Entgangene9bEur(250.0, 1179.7, SATZ_9B, 250.0, 0.0));
        }

        // =====================================================================
        //  Der Lauf (Testdatenbank)
        // =====================================================================

        /// <summary>
        /// <b>Ohne gepflegten Anteil</b> (der Netzträger 60 führt in 1030 keinen): Obergrenze ist der
        /// Regelsatz 20,50 €/MWh ≥ 20,00 — kein Deckel, die Korrektur ist bitgleich die von vor
        /// EZ‑23: 432,3 MWh × 20,00 = 8.646,00 €/a.
        /// </summary>
        [Fact]
        public void Ohne_gepflegten_Anteil_gilt_der_Regelsatz_als_Obergrenze_ohne_Deckel()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            WirtschaftlichkeitErgebnis e = Rechne();
            Assert.True(e.ProduzierendesGewerbe);
            Assert.Equal(MENGE_MWH, e.VermiedenMengeMWh, 6);
            Assert.True(e.StromsteuerEntlastungJahr1 > 0);              // der Netzbezug trägt den Sockel
            Assert.Equal(SATZ_9B * e.VermiedenMengeMWh, e.VermiedenEntlastung9bJahr);
            Assert.Equal(8646.0, e.VermiedenEntlastung9bJahr, 2);
            // Der Nachweis des Ausweises schweigt — die Obergrenze wirkt nicht (die Gestehung nennt
            // sie mit ihrem eigenen Text, EZ‑22).
            Assert.DoesNotContain("Vermiedene Stromkosten: ", e.Hinweis ?? "");
        }

        /// <summary>
        /// <b>Anteil 1,00 ct/kWh</b> am Träger der Bezugsrolle: Die Korrektur halbiert sich gegenüber
        /// dem § 9b-Satz — 432,3 MWh × min(20,00; 10,00) = 4.323,00 statt 8.646,00 €/a; der wirksame
        /// Betrag steigt um dieselbe Zahl. Kapitalwert und Entlastung des Projekts bleiben (der
        /// Ausweis ist kein Zahlungsstrom).
        /// </summary>
        [Fact]
        public void Mit_Anteil_1_ct_halbiert_der_Deckel_die_Korrektur()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            WirtschaftlichkeitErgebnis ohne = Rechne();
            Stromsteueranteil(1.00, true);
            WirtschaftlichkeitErgebnis mit = Rechne();

            Assert.Equal(4323.0, mit.VermiedenEntlastung9bJahr, 2);
            Assert.Equal(ohne.VermiedenEntlastung9bJahr / 2.0, mit.VermiedenEntlastung9bJahr, 6);
            Assert.Equal(ohne.VermiedenGesamtJahr, mit.VermiedenGesamtJahr);
            Assert.Equal(ohne.VermiedenEffektivJahr + 4323.0, mit.VermiedenEffektivJahr, 2);
            Assert.Equal(ohne.Kapitalwert.Value, mit.Kapitalwert.Value, 2);
            Assert.Equal(ohne.StromsteuerEntlastungJahr1, mit.StromsteuerEntlastungJahr1);

            // Der Nachweis nennt den Deckel am Laufhinweis (Punkt 2), ohne Deckel steht er nicht.
            Assert.Contains("Vermiedene Stromkosten: Die § 9b-Korrektur rechnet mit 10,00 €/MWh statt " +
                            "20,00 €/MWh — gedeckelt auf den Stromsteueranteil des Arbeitspreises des " +
                            "Netzstromträgers.", mit.Hinweis);
            Assert.DoesNotContain("§ 9b-Korrektur", ohne.Hinweis ?? "");
        }

        /// <summary>
        /// <b>Abgeschaltete Komponente</b>: Der Preis enthält keine Stromsteuer (Anteil 0) — es
        /// entgeht keine Entlastung, die Korrektur ist 0 und der Ausweis zeigt brutto = wirksam.
        /// </summary>
        [Fact]
        public void Mit_abgeschalteter_Komponente_entfaellt_die_Korrektur()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Stromsteueranteil(1.00, false);
            WirtschaftlichkeitErgebnis e = Rechne();
            Assert.True(e.ProduzierendesGewerbe);
            Assert.Equal(MENGE_MWH, e.VermiedenMengeMWh, 6);
            Assert.Equal(0.0, e.VermiedenEntlastung9bJahr);
            Assert.Equal(e.VermiedenGesamtJahr, e.VermiedenEffektivJahr);
            Assert.Contains("Die § 9b-Korrektur rechnet mit 0,00 €/MWh statt 20,00 €/MWh", e.Hinweis);
        }

        /// <summary>
        /// <b>Ein Anteil ab dem Satz</b> (2,05 ct/kWh wie im Beispielprojekt) lässt den Satz
        /// unberührt — bitgleich zum Lauf ohne gepflegten Anteil.
        /// </summary>
        [Fact]
        public void Ein_Anteil_ab_dem_Satz_laesst_die_Korrektur_bitgleich()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            WirtschaftlichkeitErgebnis ohne = Rechne();
            Stromsteueranteil(2.05, true);
            WirtschaftlichkeitErgebnis mit = Rechne();
            Assert.Equal(ohne.VermiedenEntlastung9bJahr, mit.VermiedenEntlastung9bJahr);
            Assert.Equal(SATZ_9B * MENGE_MWH, mit.VermiedenEntlastung9bJahr, 6);
            Assert.DoesNotContain("§ 9b-Korrektur", mit.Hinweis ?? "");
        }

        // =====================================================================
        //  Der Nachweis (ohne Datenbank)
        // =====================================================================

        /// <summary>
        /// <b>Der Nachweis nennt den Deckel nur, wenn er wirkt.</b> Gepflegter Anteil unter dem Satz
        /// → <c>WIRT_VERMIEDEN_9B_DECKEL</c> (auch Anteil 0); kein Anteil und ein Regelsatz unter dem
        /// Satz → <c>WIRT_VERMIEDEN_9B_REGELSATZ</c>; Anteil oder Regelsatz ab dem Satz, kein Satz →
        /// kein Text.
        /// </summary>
        [Fact]
        public void Der_Nachweis_nennt_den_Deckel_nur_wenn_er_wirkt()
        {
            var de = System.Globalization.CultureInfo.GetCultureInfo("de-DE");
            Assert.Equal("Vermiedene Stromkosten: Die § 9b-Korrektur rechnet mit 10,00 €/MWh statt 20,00 €/MWh — " +
                         "gedeckelt auf den Stromsteueranteil des Arbeitspreises des Netzstromträgers.",
                         WirtschaftlichkeitCtrl.Nachweis9bAusweis(20.0, 10.0, 20.50, de));
            Assert.Contains("mit 0,00 €/MWh statt 20,00 €/MWh",
                            WirtschaftlichkeitCtrl.Nachweis9bAusweis(20.0, 0.0, 20.50, de));
            Assert.Equal("Vermiedene Stromkosten: Für den Netzstromträger ist kein Stromsteueranteil gepflegt; die " +
                         "§ 9b-Korrektur rechnet mit dem Regelsatz der Stromsteuer von 15,00 €/MWh statt 20,00 €/MWh.",
                         WirtschaftlichkeitCtrl.Nachweis9bAusweis(20.0, null, 15.0, de));

            Assert.Null(WirtschaftlichkeitCtrl.Nachweis9bAusweis(20.0, 20.50, 20.50, de));   // Anteil ab dem Satz
            Assert.Null(WirtschaftlichkeitCtrl.Nachweis9bAusweis(20.0, 20.0, 20.50, de));    // gleich: wirkt nicht
            Assert.Null(WirtschaftlichkeitCtrl.Nachweis9bAusweis(20.0, null, 20.50, de));    // Regelsatz ab dem Satz
            Assert.Null(WirtschaftlichkeitCtrl.Nachweis9bAusweis(20.0, null, null, de));     // nichts gepflegt
            Assert.Null(WirtschaftlichkeitCtrl.Nachweis9bAusweis(null, 10.0, 20.50, de));    // kein Satz
            Assert.Null(WirtschaftlichkeitCtrl.Nachweis9bAusweis(0.0, 10.0, 20.50, de));

            // Die Zahl folgt derselben Regel: Der Regelsatz unter dem Satz deckelt auch die Korrektur.
            Assert.Equal(1000.0 * 15.0, SteuerGutschriftRechner.Entgangene9bEur(500.0, 1000.0, 20.0, 250.0, 15.0), 6);
        }

        // =====================================================================
        //  Hilfen
        // =====================================================================

        /// <summary>1030 mit den Stundenreihen des Prüffalls B6, aktiver Tarifstruktur im
        /// Rollenmodell (Bezug 0,30, Reststrom 0,32 €/kWh, ohne Leistungspreis) und produzierendem
        /// Gewerbe; das Ergebnis des erwarteten Szenarios.</summary>
        private static WirtschaftlichkeitErgebnis Rechne()
        {
            var ctrl = new WirtschaftlichkeitCtrl();
            var t = new TarifParameter { IdStamm = PROJEKT, Aktiv = true, Modus = DbWerte.TARIF_MODUS_ROLLEN };
            t.Bezug.ArbeitspreisEurKWh = 0.30;
            t.Reststrom.ArbeitspreisEurKWh = 0.32;
            Assert.True(ctrl.SpeichereTarif(t));
            Assert.True(ctrl.LadeTarif(PROJEKT).Wirksam);

            WirtschaftlichkeitParameter p = ctrl.LadeParameter(PROJEKT);
            p.Unternehmensart = DbWerte.UNTERNEHMENSART_PROD_GEWERBE;
            var v = new VariantenDaten
            {
                IdProjekt = PROJEKT,
                IstStamm = true,
                Projektname = "Ausweis § 9b",
                Ergebnis = new ErgebnisCtrl().Load(PROJEKT),
                Zeitreihen = StromsteuerBefreiungModusTests.Stundenreihen()
            };
            KostenEmissionRechner.Berechne(v);
            var daten = new BerichtsDaten { IdStamm = PROJEKT, Stammprojektname = v.Projektname };
            daten.Varianten.Add(v);
            WirtschaftlichkeitErgebnis e = new WirtschaftlichkeitCtrl().Berechne(daten, p).FirstOrDefault(
                x => x.Szenario == WirtschaftlichkeitSzenario.ERWARTET && x.IdProjekt == PROJEKT);
            Assert.NotNull(e);
            Assert.Null(e.Fehlgrund);
            return e;
        }

        /// <summary>Setzt den Stromsteueranteil des Netzträgers im Projekt („Strompreis Details").</summary>
        private static void Stromsteueranteil(double ctJeKwh, bool aktiv)
        {
            DataRepository.ExecuteNonQuery(
                "UPDATE energy_project_settings SET Aufschlag_Stromsteuer = ?, Aufschlag_Stromsteuer_Aktiv = ? " +
                "WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                new DbParam("@w", ctJeKwh), new DbParam("@a", aktiv ? 1 : 0),
                new DbParam("@p", PROJEKT), new DbParam("@c", NETZTRAEGER));
        }
    }
}
