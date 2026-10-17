using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die drei Leistungsarten im BETRIEBSRASTER</b> — „je kW Leistung", „je kW
    /// Heizleistung" und „je kW elektrisch".
    ///
    /// <para><b>Warum sie dort hingehören.</b> Wartung und Instandhaltung werden
    /// branchenüblich je installierter Leistung bemessen. Bis hierher bot das
    /// Betriebsraster dafür nur den festen Jahresbetrag, „% der Investition" und die
    /// Mengenarten aus dem Lauf — eine Wartungspauschale von 25,00 € je kW Heizleistung
    /// ließ sich nicht bemessen, sie mußte als fester Jahresbetrag ausgerechnet
    /// eingetragen werden und wanderte damit nicht mit der Anlagengröße.</para>
    ///
    /// <para><b>Ohne eine einzige neue Formel.</b> <see cref="BetriebskostenCtrl.Betrag"/>
    /// kennt die drei Arten längst (Menge × Satz), und die Bezugsgröße kommt aus
    /// derselben Landkarte wie auf der Investitionsseite
    /// (<see cref="TechnikPlanwertCtrl.KenntBaugroesse"/>). Genau deshalb ist die
    /// Herkunft der Betriebszeile die ANLAGE und nicht der Lauf.</para>
    ///
    /// <para><b>Das Jahr steht im SATZ.</b> Eine Leistung kennt kein Jahr, ein
    /// Betriebssatz muß es deshalb selbst tragen: „€/kW·a". Die Bezugsgröße bleibt
    /// davon unberührt — sie wird in kW gemessen, nicht in kW·a. Dasselbe Muster wie
    /// bei „je kWp Leistung" (<c>KwpBemessungHerleitungTests</c>).</para>
    ///
    /// <para>Welche Gewerke die Auswahl damit gewinnen, steht in
    /// <c>BemessungsauswahlJeGewerkTests</c>: die sieben mit einer Leistungsgröße.</para>
    /// </summary>
    public class LeistungsbemessungBetriebTests
    {
        private const int K_WAERMEPUMPE = 1;
        private const int K_HEIZKESSEL = 2;
        private const int K_PHOTOVOLTAIK = 3;
        private const int K_STROMSPEICHER = 5;
        private const int K_PUFFERSPEICHER = 6;
        private const int K_BHKW = 7;

        // =====================================================================
        //  Der Betrag: je Art ein Fall
        // =====================================================================

        /// <summary>
        /// Je Art dieselbe Zahlenprobe: 12,00 €/kW·a × 300,00 kW = 3.600,00 €/a. Der
        /// Rechenweg ist für alle drei derselbe wie auf der Investitionsseite — Menge ×
        /// Satz —, nur daß der Satz ein Jahressatz ist.
        /// </summary>
        [Theory]
        [InlineData(DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG)]
        [InlineData(DbWerte.BEMESSUNG_EUR_PRO_KW_HEIZLEISTUNG)]
        [InlineData(DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH)]
        public void Ein_Leistungssatz_im_Betriebsraster_rechnet_Satz_mal_Leistung(string bemessung)
        {
            Assert.Equal(3600.00,
                BetriebskostenCtrl.Betrag(bemessung, 0.0, 300.0, 12.0, false), 2);

            // Gegenprobe zum Anwenderentscheid I-2: Ohne Bezugsgröße ist die Ableitung
            // nicht rechenbar, dann gilt der erfaßte Betrag — nie stillschweigend 0.
            Assert.Equal(480.00,
                BetriebskostenCtrl.Betrag(bemessung, 480.0, null, 12.0, false), 2);
        }

        /// <summary>
        /// Eine Erlöszeile kehrt das Vorzeichen um — dieselbe Klammer wie bei jeder
        /// anderen bemessenen Art.
        /// </summary>
        [Theory]
        [InlineData(DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG)]
        [InlineData(DbWerte.BEMESSUNG_EUR_PRO_KW_HEIZLEISTUNG)]
        [InlineData(DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH)]
        public void Ein_Leistungssatz_als_Erloes_zaehlt_negativ(string bemessung)
        {
            Assert.Equal(-3600.00,
                BetriebskostenCtrl.Betrag(bemessung, 0.0, 300.0, 12.0, true), 2);
        }

        // =====================================================================
        //  Die Einheit folgt dem Raster
        // =====================================================================

        /// <summary>
        /// Je Art und Gewerk: Auf der Investitionsseite „€/kW", im Betriebsraster
        /// „€/kW·a". Ohne das Jahr im Zeichen stünde hinter 12,00 dieselbe Einheit wie
        /// bei einer einmaligen Investition, daneben aber ein Jahresbetrag.
        /// </summary>
        [Theory]
        [InlineData(K_WAERMEPUMPE, DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG)]
        [InlineData(K_WAERMEPUMPE, DbWerte.BEMESSUNG_EUR_PRO_KW_HEIZLEISTUNG)]
        [InlineData(K_HEIZKESSEL, DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG)]
        [InlineData(K_HEIZKESSEL, DbWerte.BEMESSUNG_EUR_PRO_KW_HEIZLEISTUNG)]
        [InlineData(K_PHOTOVOLTAIK, DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH)]
        [InlineData(K_STROMSPEICHER, DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG)]
        [InlineData(K_STROMSPEICHER, DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH)]
        [InlineData(K_BHKW, DbWerte.BEMESSUNG_EUR_PRO_KW_HEIZLEISTUNG)]
        [InlineData(K_BHKW, DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH)]
        public void Die_Leistungsarten_tragen_im_Betriebsraster_die_Jahreseinheit(
            int komponente, string bemessung)
        {
            Assert.Equal("€/kW", BemessungKatalog.Einheit(bemessung, komponente, false));
            Assert.Equal("€/kW·a", BemessungKatalog.Einheit(bemessung, komponente, true));

            // Dieselbe Antwort am Satzfeld — EINE Wahrheit, zwei Einstiege.
            Assert.Equal("€/kW", BetriebskostenCtrl.SatzEinheit(bemessung, komponente));
            Assert.Equal("€/kW·a", BetriebskostenCtrl.SatzEinheit(bemessung, komponente, true));
        }

        /// <summary>
        /// Auch die beiden gewerk-eigenen Beschriftungen tragen ihr Jahr: Am BHKW heißt
        /// „je kW Leistung" „je kW elektr. Leistung", am Pufferspeicher „je Liter" —
        /// beide Bezugsgrößen kennen so wenig ein Jahr wie eine kW-Zahl.
        /// </summary>
        [Fact]
        public void Die_gewerkeigenen_Beschriftungen_tragen_ihr_Jahr_mit()
        {
            Assert.Equal("€/kW", BemessungKatalog.Einheit(
                DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG, K_BHKW, false));
            Assert.Equal("€/kW·a", BemessungKatalog.Einheit(
                DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG, K_BHKW, true));

            Assert.Equal("€/Ltr.", BemessungKatalog.Einheit(
                DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG, K_PUFFERSPEICHER, false));
            Assert.Equal("€/Ltr.·a", BemessungKatalog.Einheit(
                DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG, K_PUFFERSPEICHER, true));
        }

        /// <summary>
        /// Gegenprobe: Die MENGENARTEN tragen ihr Jahr in der Bezugsgröße [kWh/a] und
        /// heißen deshalb in beiden Rastern gleich. Nur eine LEISTUNG braucht das
        /// Zeichen im Satz.
        /// </summary>
        [Fact]
        public void Die_Mengenarten_bleiben_in_beiden_Rastern_gleich()
        {
            Assert.Equal("€/kWh", BemessungKatalog.Einheit(
                DbWerte.BEMESSUNG_EUR_PRO_KWH_THERMISCH, K_HEIZKESSEL, true));
            Assert.Equal("€/kWh", BemessungKatalog.Einheit(
                DbWerte.BEMESSUNG_EUR_PRO_KWH_ELEKTRISCH, K_BHKW, true));
        }

        // =====================================================================
        //  Herkunft und Herleitungszeile
        // =====================================================================

        /// <summary>
        /// Die Bezugsgröße kommt aus der GERÄTEWELT, nicht aus dem Lauf — deshalb
        /// antwortet die Landkarte für alle drei Arten an ihren Gewerken mit „ja".
        /// Genau daran hängt die Herkunft <c>HERKUNFT_ANLAGE</c> der Betriebszeile.
        /// </summary>
        [Theory]
        [InlineData(K_WAERMEPUMPE, DbWerte.BEMESSUNG_EUR_PRO_KW_HEIZLEISTUNG)]
        [InlineData(K_HEIZKESSEL, DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG)]
        [InlineData(K_PHOTOVOLTAIK, DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH)]
        [InlineData(K_STROMSPEICHER, DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG)]
        [InlineData(K_PUFFERSPEICHER, DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG)]
        [InlineData(K_BHKW, DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH)]
        public void Die_Bezugsgroesse_kommt_aus_der_Geraetewelt(int komponente, string bemessung)
        {
            Assert.True(TechnikPlanwertCtrl.KenntBaugroesse(komponente, bemessung));
        }

        /// <summary>
        /// Werkzeugtipp und Herleitungszeile einer Wartungszeile am Heizkessel: Der SATZ
        /// steht in der Jahreseinheit, die BEZUGSGRÖSSE in ihrer physikalischen —
        /// „× 300,00 kW · P_therm der Anlage". Ohne Kaskade gibt es keine Runde
        /// (Betriebsseite).
        /// </summary>
        [Fact]
        public void Die_Betriebszeile_nennt_Jahressatz_und_die_Leistung_der_Anlage()
        {
            using var kultur = new Kulturvorrichtung();

            var p = new KostenVorlagenPosition
            {
                Bezeichnung = "Wartung / Inspektion Kessel",
                Bemessung = DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG,
                Satz = 12.0,
                BetragNetto = 3600.0
            };
            var pz = new KostenProjektPositionenCtrl.Zeile
            {
                Basis = 300.0,
                Runde = 0,
                BasisHerkunft = KostenHerleitung.HERKUNFT_ANLAGE
            };

            KostenHerleitung.Angabe a =
                KostenHerleitung.Bilde(p, K_HEIZKESSEL, pz, true, betrieb: true);

            Assert.Equal("300,00 kW", a.BasisText);
            Assert.Equal("× 300,00 kW · P_therm der Anlage", a.Zeile);
            Assert.Contains("12,00 €/kW·a × 300,00 kW", a.Kurztext);
        }

        /// <summary>
        /// Gegenprobe zum Raster: Dieselbe Zeile auf der INVESTITIONSSEITE trägt den
        /// Satz ohne Jahr. Ein €/kW-Satz kauft den Kessel einmal, ein €/kW·a-Satz wartet
        /// ihn jedes Jahr — die Bezugsgröße bleibt in beiden Fällen dieselbe.
        /// </summary>
        [Fact]
        public void Dieselbe_Art_traegt_auf_der_Investitionsseite_kein_Jahr()
        {
            using var kultur = new Kulturvorrichtung();

            var p = new KostenVorlagenPosition
            {
                Bezeichnung = "Wärmeerzeuger (Kessel)",
                Bemessung = DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG,
                Satz = 250.0,
                BetragNetto = 75000.0
            };
            var pz = new KostenProjektPositionenCtrl.Zeile
            {
                Basis = 300.0,
                Runde = 1,
                BasisHerkunft = KostenHerleitung.HERKUNFT_ANLAGE
            };

            KostenHerleitung.Angabe a = KostenHerleitung.Bilde(p, K_HEIZKESSEL, pz, true);

            Assert.Equal("× 300,00 kW · P_therm der Anlage · Runde 1", a.Zeile);
            Assert.Contains("250,00 €/kW × 300,00 kW", a.Kurztext);
        }
    }
}
