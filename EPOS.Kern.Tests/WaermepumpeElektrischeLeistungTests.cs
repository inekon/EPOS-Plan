using System;
using System.Collections.Generic;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// E20 — „je kW elektrisch" an der Wärmepumpe (Konzept § 6.3 Nr. 10, Anwenderentscheid
    /// 25.09.2026: „‚Wärmepumpe beides' nur bei Investitionskosten nach kW elektrisch und
    /// kW thermisch").
    ///
    /// <para><b>Die Bezugsgröße wird gerechnet.</b> <c>Tab_WP</c> führt keine elektrische
    /// Leistung; sie kommt aus der Kennlinie als Ptherm ÷ COP am NORMPUNKT bei 35 °C
    /// Vorlauf — Luft/Wasser A2/W35, Sole/Wasser B0/W35, Wasser/Wasser W10/W35
    /// (E20‑Q1 a, Q2 a). Fehlt die Stützstelle, wird zwischen den Nachbarn interpoliert,
    /// nie extrapoliert. Heizstab und Kühlbetrieb zählen nicht (Q3 a, Q4 a).</para>
    ///
    /// <para><b>Nur Kategorie 1.</b> Auf der Betriebsseite bleibt die Art an der
    /// Wärmepumpe ohne Bezugsgröße (Grund GEWERK, Q5 a).</para>
    ///
    /// <para>Gemessen an den Projektkopien der Testdatenbank: 1024 (Luft/Wasser,
    /// A2/W35 = 11,6 kW bei COP 2,9 ⇒ 4,00 kW), 1017 (Sole/Wasser, B0/W35 = 35,6 kW bei
    /// COP 4,5 ⇒ 7,91 kW), 1006 (Luft/Wasser ohne A2-Stützstelle — zwischen A0 und A5
    /// interpoliert ⇒ 8,75 kW).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class WaermepumpeElektrischeLeistungTests : IDisposable
    {
        private const int K_WP = 1;

        private const int P_1024 = 1024;
        private const int A_1024 = 11262;        // Anlagenzeile der Wärmepumpe
        private const int WP_1024 = 1034317;     // Projektgerät, Luft/Wasser

        private const int P_1017 = 1017;
        private const int A_1017 = 10211;        // Sole/Wasser mit Kühlbetrieb

        private const int P_1006 = 1006;
        private const int A_1006 = 10114;        // Luft/Wasser, W35 nur bei −5/0/5/10 °C
        private const int WP_1006 = 1006020;

        // Positionen der Fälle — oberhalb des Bestands der Testdatenbank.
        private const int POS_INVEST = 202000001;
        private const int POS_BETRIEB = 202000002;
        private const int STAMM_INVEST = 77;     // „Wärmepumpe" der Kategorie 1 in 1024
        private const int STAMM_BETRIEB = 85;    // die Betriebszeile der WP in 1024

        private const double SATZ = 1000.0;      // €/kW  ⇒ 4.000,00 € an 1024

        /// <summary>P_el an den Normpunkten, gerechnet aus den gespeicherten Stützstellen
        /// (die REAL-Spalten der Kennlinie tragen Werte mit einfacher Genauigkeit).</summary>
        private const double PEL_1024 = 4.0;
        private const double PEL_1017 = 35.6 / 4.5;
        private static readonly double PEL_1006 = Interpoliert(
            41.689998626708984, 4.71999979019165,     // A0/W35
            48.09000015258789, 5.559999942779541,     // A5/W35
            0.4);

        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose()
        {
            _kultur.Dispose();
        }

        private static double Interpoliert(double pth0, double cop0, double pth1, double cop1, double f)
        {
            return (pth0 + f * (pth1 - pth0)) / (cop0 + f * (cop1 - cop0));
        }

        // =====================================================================
        // Die Bezugsgröße am Normpunkt
        // =====================================================================

        [Fact]
        public void Luft_Wasser_nimmt_A2_W35()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            TechnikPlanwertCtrl.WpNormpunkt n =
                TechnikPlanwertCtrl.WaermepumpeNormpunkt(WP_1024, DbWerte.WP_BAUART_LUFT_WASSER);
            Assert.NotNull(n);
            Assert.Equal("A2/W35", n.Name);
            Assert.False(n.Interpoliert);
            Assert.Equal(PEL_1024, n.PelKw, 6);

            Assert.Equal(PEL_1024, TechnikPlanwertCtrl.WaermepumpePelKw(P_1024, A_1024).Value, 6);
            Assert.Equal(PEL_1024, TechnikPlanwertCtrl.BaugroesseSumme(
                P_1024, K_WP, DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH, A_1024, true).Value, 6);
        }

        /// <summary>Sole/Wasser B0/W35 — auch an der Wärmepumpe im Kühlbetrieb (1017):
        /// die Kühlkennlinie zählt nicht (E20‑Q4 a).</summary>
        [Fact]
        public void Sole_Wasser_nimmt_B0_W35_auch_im_Kuehlbetrieb()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Equal(PEL_1017, TechnikPlanwertCtrl.WaermepumpePelKw(P_1017, A_1017).Value, 4);
            Assert.Equal(PEL_1017, TechnikPlanwertCtrl.BaugroesseSumme(
                P_1017, K_WP, DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH, 0, true).Value, 4);
        }

        /// <summary>Die Normtemperatur je Bauart; eine unbekannte Bauart hat keine.</summary>
        [Fact]
        public void Die_Normtemperatur_folgt_der_Bauart()
        {
            Assert.Equal(2, TechnikPlanwertCtrl.WpNormQuellentemperatur(DbWerte.WP_BAUART_LUFT_WASSER));
            Assert.Equal(0, TechnikPlanwertCtrl.WpNormQuellentemperatur(DbWerte.WP_BAUART_SOLE_WASSER));
            Assert.Equal(10, TechnikPlanwertCtrl.WpNormQuellentemperatur(DbWerte.WP_BAUART_WASSER_WASSER));
            Assert.Null(TechnikPlanwertCtrl.WpNormQuellentemperatur(null));
            Assert.Null(TechnikPlanwertCtrl.WpNormQuellentemperatur("Luft-Luft"));
        }

        /// <summary>Fehlt die A2-Stützstelle (T 800-2: nur −5/0/5/10 °C bei W35), wird
        /// zwischen A0 und A5 interpoliert — Heizleistung und COP je für sich.</summary>
        [Fact]
        public void Ohne_Stuetzstelle_wird_zwischen_den_Nachbarn_interpoliert()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            TechnikPlanwertCtrl.WpNormpunkt n =
                TechnikPlanwertCtrl.WaermepumpeNormpunkt(WP_1006, DbWerte.WP_BAUART_LUFT_WASSER);
            Assert.NotNull(n);
            Assert.True(n.Interpoliert);
            Assert.Equal("A2/W35", n.Name);
            Assert.Equal(PEL_1006, n.PelKw, 6);
            Assert.Equal(PEL_1006, TechnikPlanwertCtrl.WaermepumpePelKw(P_1006, A_1006).Value, 6);
        }

        /// <summary>Nie extrapolieren: Liegt der Normpunkt außerhalb der Kennlinie, gibt es
        /// keine Bezugsgröße — und der Grund nennt das Gerät (E20‑Q8 a).</summary>
        [Fact]
        public void Ohne_umschliessende_Stuetzstellen_gibt_es_keine_Bezugsgroesse()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            DataRepository.ExecuteSQL(
                "DELETE FROM Tab_Kenndaten WHERE ID_WP = " + WP_1024 + " AND Vorlauf = 35 AND Temperatur <= 2");

            Assert.Null(TechnikPlanwertCtrl.WaermepumpeNormpunkt(WP_1024, DbWerte.WP_BAUART_LUFT_WASSER));
            Assert.Null(TechnikPlanwertCtrl.BaugroesseSumme(
                P_1024, K_WP, DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH, A_1024, true));

            PositionAnlegen(POS_INVEST, DbWerte.KOSTEN_KATEGORIE_INVESTITION, STAMM_INVEST,
                            DbWerte.KOSTENART_KAPITALGEBUNDEN);
            string grund;
            Assert.Null(WirtschaftlichkeitCtrl.FrischeBasis(POS_INVEST, null, out grund));
            Assert.Equal(WirtschaftlichkeitCtrl.BASISGRUND_GERAET, grund);
        }

        /// <summary>COP 0 ist keine Leistungszahl — ein solcher Punkt zählt nicht.</summary>
        [Fact]
        public void COP_null_liefert_keine_Bezugsgroesse()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            DataRepository.ExecuteSQL(
                "UPDATE Tab_Kenndaten SET COP = 0 WHERE ID_WP = " + WP_1024 + " AND Vorlauf = 35");

            Assert.Null(TechnikPlanwertCtrl.WaermepumpeNormpunkt(WP_1024, DbWerte.WP_BAUART_LUFT_WASSER));
            Assert.Null(TechnikPlanwertCtrl.WaermepumpePelKw(P_1024, A_1024));
        }

        /// <summary>Ohne Bauart kein Normpunkt.</summary>
        [Fact]
        public void Ohne_Bauart_gibt_es_keine_Bezugsgroesse()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            DataRepository.ExecuteSQL("UPDATE Tab_WP SET Typ = NULL WHERE ID = " + WP_1024);
            Assert.Null(TechnikPlanwertCtrl.WaermepumpePelKw(P_1024, A_1024));
        }

        // =====================================================================
        // Die Kategoriebindung
        // =====================================================================

        /// <summary>Die Fassung ohne Raster ist die der Betriebsseite — dort bleibt die
        /// Art an der Wärmepumpe ohne Bezugsgröße, wie vor E20.</summary>
        [Fact]
        public void Ohne_Raster_und_im_Betrieb_bleibt_es_beim_Gewerk()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Null(TechnikPlanwertCtrl.BaugroesseSumme(
                P_1024, K_WP, DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH, A_1024));
            Assert.Null(TechnikPlanwertCtrl.BaugroesseSumme(
                P_1024, K_WP, DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH, A_1024, false));
            Assert.Equal("", TechnikPlanwertCtrl.BaugroesseHerleitung(
                P_1024, K_WP, DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH, A_1024));
        }

        /// <summary>
        /// DER ENTSCHEID im Rechenweg: Eine Investitionszeile „je kW elektrisch" an der
        /// Wärmepumpe von 1024 mit 1.000 €/kW ergibt 4,00 kW × 1.000 €/kW = 4.000,00 € —
        /// aus der Investitionskaskade, Herkunft „Anlage". Dieselbe Art als
        /// Betriebszeile bleibt ohne Bezugsgröße (Grund GEWERK), ihr Betrag 0.
        /// </summary>
        [Fact]
        public void Investition_rechnet_mit_P_el_der_Betrieb_nicht()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            PositionAnlegen(POS_INVEST, DbWerte.KOSTEN_KATEGORIE_INVESTITION, STAMM_INVEST,
                            DbWerte.KOSTENART_KAPITALGEBUNDEN);
            PositionAnlegen(POS_BETRIEB, DbWerte.KOSTEN_KATEGORIE_BETRIEB, STAMM_BETRIEB,
                            DbWerte.KOSTENART_BETRIEBSGEBUNDEN);

            InvestKaskade.Zeile k =
                InvestKaskade.NachId(P_1024, WirtschaftlichkeitSzenario.ERWARTET)[POS_INVEST];
            Assert.Equal(PEL_1024, k.Basis.Value, 6);
            Assert.Equal(PEL_1024 * SATZ, k.Betrag, 6);
            Assert.Equal(KostenHerleitung.HERKUNFT_ANLAGE, k.Herkunft);

            KostenPositionNachweis n =
                WirtschaftlichkeitCtrl.BetriebNachId(P_1024, WirtschaftlichkeitSzenario.ERWARTET)[POS_BETRIEB];
            Assert.Null(n.Menge);
            Assert.Equal(0.0, n.BetragJahr, 6);

            string grund;
            Assert.Equal(PEL_1024, WirtschaftlichkeitCtrl.FrischeBasis(POS_INVEST, null, out grund).Value, 6);
            Assert.Equal("", grund);
            Assert.Null(WirtschaftlichkeitCtrl.FrischeBasis(POS_BETRIEB, null, out grund));
            Assert.Equal(WirtschaftlichkeitCtrl.BASISGRUND_GEWERK, grund);
        }

        /// <summary>Der Dialog: Die Investitionszeile nennt Bezugsgröße, Herkunft und
        /// Herleitung; die Betriebszeile den Grund GEWERK und keine Herleitung.</summary>
        [Fact]
        public void Der_Dialog_nennt_Normpunkt_und_Herkunft()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            PositionAnlegen(POS_INVEST, DbWerte.KOSTEN_KATEGORIE_INVESTITION, STAMM_INVEST,
                            DbWerte.KOSTENART_KAPITALGEBUNDEN);
            PositionAnlegen(POS_BETRIEB, DbWerte.KOSTEN_KATEGORIE_BETRIEB, STAMM_BETRIEB,
                            DbWerte.KOSTENART_BETRIEBSGEBUNDEN);

            KostenProjektPositionenCtrl.Zeile inv = Zeile(
                KostenProjektPositionenCtrl.Lies(P_1024, K_WP, DbWerte.KOSTEN_KATEGORIE_INVESTITION), POS_INVEST);
            Assert.Equal(PEL_1024, inv.Basis.Value, 6);
            Assert.Equal(KostenHerleitung.HERKUNFT_ANLAGE, inv.BasisHerkunft);
            Assert.Equal("11,60 kW ÷ COP 2,90 (A2/W35) = 4,00 kW", inv.BasisHerleitung);
            Assert.Equal("", inv.BasisGrund);

            KostenProjektPositionenCtrl.Zeile bet = Zeile(
                KostenProjektPositionenCtrl.Lies(P_1024, K_WP, DbWerte.KOSTEN_KATEGORIE_BETRIEB), POS_BETRIEB);
            Assert.False(bet.Basis.HasValue);
            Assert.Equal(WirtschaftlichkeitCtrl.BASISGRUND_GEWERK, bet.BasisGrund);
            Assert.Equal("", bet.BasisHerleitung);
        }

        // =====================================================================
        // Herleitung und Name
        // =====================================================================

        [Fact]
        public void Die_Herleitung_nennt_Normpunkt_und_Rechnung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Equal("11,60 kW ÷ COP 2,90 (A2/W35) = 4,00 kW",
                         TechnikPlanwertCtrl.BaugroesseHerleitung(
                             P_1024, K_WP, DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH, A_1024, true));
            Assert.Equal("35,60 kW ÷ COP 4,50 (B0/W35) = 7,91 kW",
                         TechnikPlanwertCtrl.WaermepumpePelHerleitung(P_1017, A_1017));
            // Interpoliert: „≈" vor Heizleistung und COP.
            Assert.Equal("≈44,25 kW ÷ COP ≈5,06 (A2/W35) = 8,75 kW",
                         TechnikPlanwertCtrl.WaermepumpePelHerleitung(P_1006, A_1006));
        }

        /// <summary>Projektsicht mit mehreren Wärmepumpen (1009: drei Geräte) — die
        /// Summe, nicht zehn Einzelrechnungen im Kurztext.</summary>
        [Fact]
        public void Mehrere_Waermepumpen_werden_zur_Summe()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            double summe = TechnikPlanwertCtrl.WaermepumpePelKw(1009, 0).Value;
            Assert.Equal("Σ P_el am Normpunkt von 3 Wärmepumpen = " +
                         summe.ToString("#,##0.00", CultureInfo.CurrentCulture) + " kW",
                         TechnikPlanwertCtrl.WaermepumpePelHerleitung(1009, 0));
        }

        [Fact]
        public void Der_Name_der_Bezugsgroesse_ist_P_el()
        {
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.KDLG_GR_PEL,
                         TechnikPlanwertCtrl.BaugroessenName(K_WP, DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH));
        }

        // =====================================================================
        // Aufbau
        // =====================================================================

        private static void PositionAnlegen(int id, int kategorie, int stamm, string kostenart)
        {
            DataRepository.ExecuteSQL(
                "INSERT INTO Tab_ProjektWerte " +
                "(ID, ProjektID, StammID, KomponentenID, KategorieID, EingegebenerWert, " +
                " Nutzungsdauer, Kostenart, Bemessung, Einheitpreis, ID_Anlage) VALUES (" +
                id + ", " + P_1024 + ", " + stamm + ", " + K_WP + ", " + kategorie + ", 0.0, 20, '" +
                kostenart + "', '" + DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH + "', " +
                SATZ.ToString(CultureInfo.InvariantCulture) + ", " + A_1024 + ")");
        }

        private static KostenProjektPositionenCtrl.Zeile Zeile(
            List<KostenProjektPositionenCtrl.Zeile> zeilen, int id)
        {
            foreach (KostenProjektPositionenCtrl.Zeile z in zeilen)
                if (z.Raster.Id == id) return z;
            throw new InvalidOperationException("Zeile " + id + " fehlt.");
        }
    }
}
