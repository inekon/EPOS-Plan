using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Nachweis der Welle 14a</b> (iU9-W14a.0i) — die Katalogverwaltung der sieben
    /// Erzeuger-Admin-Masken, gefahren auf einer Arbeitskopie der Testdatenbank.
    ///
    /// <para><b>Warum es diese Klasse gibt.</b> Befund W14-B77: <em>kein</em> Referenzlauf,
    /// <em>keine</em> ChartProbe und <em>kein</em> Kern-Test beruehrt die elf Masken der
    /// Wellen 14a/14b fachlich. Die drei vorhandenen Testanker haengen an der
    /// ERREICHBARKEIT und an der SCHREIBWEISE eines Dateinamens, nicht am Verhalten. Sieben
    /// Masken ohne Netz zu portieren waere geraten; deshalb entsteht der Nachweis
    /// <em>vor</em> der ersten portierten Zeile (Risiko R-W14-1).</para>
    ///
    /// <para><b>Die Erwartungswerte sind EINGEFROREN.</b> Sie stammen aus
    /// <c>Referenzlaeufe/Kenndaten_Test.sqlite</c> im Stand vom 04.09.2026, gemessen VOR
    /// jeder Aenderung dieser Welle. Aendert sich eine Zahl, ist das kein Testfehler,
    /// sondern eine Verhaltensaenderung der Katalogverwaltung — und dann gehoert sie als
    /// Abweichung ins Portprotokoll.</para>
    ///
    /// <para><b>Die Heizkesselzahlen stehen bewusst VOR W14a.0b.</b> Der Kern bildet heute
    /// „Sonstige" auf <c>Brennstoff=23</c> ab, die Admin-Maske dagegen
    /// <c>Fernwärme=23</c>, <c>Sonstige Energieträger=24</c>, <c>Wasserstoff=25</c>
    /// (Befund W14-B2). <see cref="Heizkessel_Brennstoffgruppen_vor_der_Berichtigung"/>
    /// haelt den Zustand VORHER fest, damit die Berichtigung in W14a.0b messbar ist; sie
    /// wird dort auf die neuen Zahlen umgestellt und der Vorher-Stand steht im Protokoll.</para>
    ///
    /// <para><b>Nur lesend, eine Arbeitskopie je Klasse</b> (Regel seit iU9-W11a):
    /// <see cref="TestDatenbank"/> als <c>IClassFixture</c>. Fehlt die Datei, schweigen
    /// die Faelle.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KatalogVerwaltungTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;

        public KatalogVerwaltungTests(TestDatenbank db) { _db = db; }

        // =================================================================================
        // 1 - Heizkessel: Form_Heizkessel_Admin.SetFilter (:89-136)
        // =================================================================================

        /// <summary>
        /// Die sechs Leistungsstufen aus <c>HeizkesselStammCtrl.LEISTUNG_SQL</c> mit
        /// eingefrorener Trefferzahl. Die Praedikate sind zeichengleich mit
        /// <c>Form_Heizkessel_Admin.cs:101-106</c>.
        /// </summary>
        [Fact]
        public void Heizkessel_Leistungsstufen_treffen_die_eingefrorenen_Zahlen()
        {
            if (!_db.Vorhanden) return;

            var ctrl = new HeizkesselStammCtrl();
            int[] erwartet = { 62, 53, 9, 0, 0, 0 };

            Assert.Equal(erwartet.Length, HeizkesselStammCtrl.LEISTUNG_SQL.Length);
            for (int stufe = 0; stufe < erwartet.Length; stufe++)
                Assert.Equal(erwartet[stufe], ctrl.Filtern("Alle", stufe).Count);
        }

        /// <summary>
        /// Die dreizehn Brennstoffgruppen VOR der Berichtigung aus W14a.0b.
        ///
        /// <para>Drei Gruppen — Fernwärme, Sonstige Energieträger, Wasserstoff — stehen in
        /// der Kern-Kette gar nicht und heben die Einengung deshalb auf: Sie liefern heute
        /// den ganzen Katalog (62), obwohl sie eine Teilmenge meinen. Genau das ist
        /// Befund W14-B2.</para>
        /// </summary>
        [Fact]
        public void Heizkessel_Brennstoffgruppen_vor_der_Berichtigung()
        {
            if (!_db.Vorhanden) return;

            var ctrl = new HeizkesselStammCtrl();
            var erwartet = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["Alle"] = 62,
                ["Gas"] = 52,
                ["Öl"] = 3,
                ["Koks"] = 0,
                ["Kohle"] = 0,
                ["Holz"] = 0,
                ["Pellets"] = 0,
                ["Strom"] = 8,
                ["Rapsöl"] = 0,
                ["Tierische Fette"] = 0,

                // Die drei Ungenauigkeiten (W14-B2): keine Einengung, also der ganze
                // Katalog. Nach W14a.0b sind es 0 / 0 / 0.
                ["Fernwärme"] = 62,
                ["Sonstige Energieträger"] = 62,
                ["Wasserstoff"] = 62
            };

            foreach (var paar in erwartet)
                Assert.Equal(paar.Value, ctrl.Filtern(paar.Key, 0).Count);
        }

        /// <summary>
        /// Die Gruppenliste der Maske kommt aus <c>Tab_BrennstoffKategorien</c> — und die
        /// fuehrt „Sonstige Energieträger", nicht „Sonstige". Der Kern-Eintrag „Sonstige"
        /// ist damit unerreichbar; diese Probe haelt die Quelle fest.
        /// </summary>
        [Fact]
        public void Heizkessel_Brennstoffgruppen_der_Maske_kennen_kein_Sonstige()
        {
            if (!_db.Vorhanden) return;

            var ctrl = new HeizkesselStammCtrl();
            Assert.Contains("Sonstige Energieträger", ctrl.Brennstoffart_Gruppe);
            Assert.Contains("Fernwärme", ctrl.Brennstoffart_Gruppe);
            Assert.Contains("Wasserstoff", ctrl.Brennstoffart_Gruppe);
            Assert.DoesNotContain("Sonstige", ctrl.Brennstoffart_Gruppe);
        }

        /// <summary>Die Liste kommt sortiert — <c>ORDER BY Bezeichner</c>.</summary>
        [Fact]
        public void Heizkessel_Katalogliste_kommt_sortiert()
        {
            if (!_db.Vorhanden) return;

            var namen = new HeizkesselStammCtrl().Filtern("Alle", 0)
                                                 .Select(z => z.Bezeichner).ToList();
            Assert.Equal(namen.OrderBy(n => n, StringComparer.Ordinal).ToList(), namen);
        }

        // =================================================================================
        // 2 - BHKW: Form_BHKWAdmin.SetFilter (:147-206)
        // =================================================================================

        /// <summary>
        /// Die ACHT Leistungsstufen plus „Alle" mit eingefrorener Trefferzahl.
        ///
        /// <para>Stufe 8 („größer 1200 kW") traf in der Maske NIE (Befund W14-B10): Die
        /// Liste kam aus <c>LeistungText</c>, verglichen wurde gegen „über 1.200 kW".
        /// Im Kern entscheidet der INDEX, also trifft sie — 8 Saetze statt der 79 des
        /// stillen Rueckfalls. Genau das ist die Abweichung, die der Port mitbringt.</para>
        /// </summary>
        [Fact]
        public void Bhkw_Leistungsstufen_treffen_die_eingefrorenen_Zahlen()
        {
            if (!_db.Vorhanden) return;

            var ctrl = new BHKWStammCtrl();
            int[] erwartet = { 79, 14, 9, 8, 10, 18, 6, 6, 8 };

            Assert.Equal(erwartet.Length, BHKWStammCtrl.LeistungFilterText.Length);
            for (int stufe = 0; stufe < erwartet.Length; stufe++)
                Assert.Equal(erwartet[stufe], ctrl.Filtern("Alle", stufe).Count);
        }

        /// <summary>
        /// Die achte Stufe ist NICHT der ganze Katalog — der Beweis, dass der
        /// Index-Weg den Bestandsfehler W14-B10 behebt.
        /// </summary>
        [Fact]
        public void Bhkw_achte_Leistungsstufe_trifft_wirklich()
        {
            if (!_db.Vorhanden) return;

            var ctrl = new BHKWStammCtrl();
            var achte = ctrl.Filtern("Alle", 8);
            var alle = ctrl.Filtern("Alle", 0);

            Assert.Equal(8, achte.Count);
            Assert.Equal(79, alle.Count);
            Assert.True(achte.Count < alle.Count);
        }

        /// <summary>Die dreizehn Brennstoffgruppen des BHKW-Filters — die RICHTIGE Kette.</summary>
        [Fact]
        public void Bhkw_Brennstoffgruppen_treffen_die_eingefrorenen_Zahlen()
        {
            if (!_db.Vorhanden) return;

            var ctrl = new BHKWStammCtrl();
            var erwartet = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["Alle"] = 79,
                ["Gas"] = 72,
                ["Öl"] = 3,
                ["Koks"] = 0,
                ["Kohle"] = 0,
                ["Holz"] = 0,
                ["Pellets"] = 0,
                ["Strom"] = 0,
                ["Rapsöl"] = 0,
                ["Tierische Fette"] = 0,
                ["Fernwärme"] = 0,
                ["Sonstige Energieträger"] = 0,
                ["Wasserstoff"] = 4
            };

            foreach (var paar in erwartet)
                Assert.Equal(paar.Value, ctrl.Filtern(paar.Key, 0).Count);
        }

        /// <summary>
        /// Die Zeile traegt die vier Werte, aus denen die Maske ihren vierzeiligen
        /// Eigenschaftentext baut (<c>Form_BHKWAdmin.cs:196-200</c>).
        /// </summary>
        [Fact]
        public void Bhkw_Katalogzeile_traegt_die_vier_Anzeigewerte()
        {
            if (!_db.Vorhanden) return;

            var zeile = new BHKWStammCtrl().Filtern("Alle", 0)
                                          .First(z => z.Bezeichner == "2G 250kw.el Gas");
            Assert.True(zeile.Id > 0);
            Assert.False(string.IsNullOrEmpty(zeile.Firma));
            Assert.False(string.IsNullOrEmpty(zeile.Brennstoff));
            Assert.True(zeile.Ptherm > 0);
            Assert.True(zeile.Pel > 0);
        }

        // =================================================================================
        // 3 - Pufferspeicher: Form_PufferSp_Admin.SetFilter (:47-71) ueber PufferSpFilter
        // =================================================================================

        /// <summary>
        /// Die sechs Volumenstufen aus <c>PufferSpStammCtrl.VOLUMEN_SQL</c> mit
        /// eingefrorener Trefferzahl.
        /// </summary>
        [Fact]
        public void PufferSp_Volumenstufen_treffen_die_eingefrorenen_Zahlen()
        {
            if (!_db.Vorhanden) return;

            var ctrl = new PufferSpStammCtrl();
            int[] erwartet = { 13, 0, 1, 5, 5, 2 };

            Assert.Equal(erwartet.Length, PufferSpStammCtrl.VOLUMEN_SQL.Length);
            for (int stufe = 0; stufe < erwartet.Length; stufe++)
                Assert.Equal(erwartet[stufe], ctrl.Filtern("", stufe).Count);

            // Stufe 0 ist wirklich ALLES: die Summe der fuenf Teilstufen ist derselbe Wert.
            Assert.Equal(erwartet[0], erwartet.Skip(1).Sum());
        }

        /// <summary>Die drei Hersteller des Katalogs, in stabiler Reihenfolge.</summary>
        [Fact]
        public void PufferSp_Hersteller_und_ihre_Trefferzahlen()
        {
            if (!_db.Vorhanden) return;

            var hersteller = PufferSpStammCtrl.Hersteller();
            Assert.Equal(new[] { "Bosch", "Vaillant Deutschland GmbH & Co. KG", "Viessmann" },
                         hersteller.ToArray());

            var ctrl = new PufferSpStammCtrl();
            Assert.Equal(3, ctrl.Filtern("Bosch", 0).Count);
            Assert.Equal(9, ctrl.Filtern("Vaillant Deutschland GmbH & Co. KG", 0).Count);
            Assert.Single(ctrl.Filtern("Viessmann", 0));
        }

        // =================================================================================
        // 4 - Photovoltaik: der Herstellerfilter des Modulkatalogs
        // =================================================================================

        [Fact]
        public void Photovoltaik_Hersteller_und_ihre_Trefferzahlen()
        {
            if (!_db.Vorhanden) return;

            var hersteller = PhotovoltaikStammCtrl.Hersteller();
            Assert.Equal(new[] { "Ablytek", "Jinkosolar", "LG Electronics", "Philadelphia Solar" },
                         hersteller.ToArray());

            var ctrl = new PhotovoltaikStammCtrl();
            Assert.Equal(6, ctrl.Filtern("Alle").Count);
            Assert.Equal(3, ctrl.Filtern("Ablytek").Count);
            Assert.Single(ctrl.Filtern("Jinkosolar"));
            Assert.Single(ctrl.Filtern("LG Electronics"));
            Assert.Single(ctrl.Filtern("Philadelphia Solar"));
        }

        // =================================================================================
        // 5 - Exists: der Vorabtest bei „Neu..."
        //
        // Die vier Browser gehen damit heute VERSCHIEDEN um (§ 12.1 der Vermessung):
        // Heizkessel prueft ueber den Controller, Pufferspeicher ueber inline-SQL
        // (W14-B27), BHKW und Solarkollektoren gar nicht. Nach W14a fragen alle vier
        // denselben Weg.
        // =================================================================================

        [Fact]
        public void Exists_kennt_die_vorhandenen_Bezeichner()
        {
            if (!_db.Vorhanden) return;

            Assert.True(new HeizkesselStammCtrl().Exists("GC7000F 22 23 - MX25"));
            Assert.True(new SolarkollektorenStammCtrl().Exists("SO4000TFV-FCC220-2V"));
            Assert.True(new PufferSpStammCtrl().Exists("Puffer 3000Ltr"));
            Assert.True(new PhotovoltaikStammCtrl().Exists("Ablytek 6MN6A270"));
            Assert.True(new StromspeicherStammCtrl().Exists("BYD B-Box HVM 11.0"));
        }

        [Fact]
        public void Exists_verneint_einen_freien_Bezeichner()
        {
            if (!_db.Vorhanden) return;

            const string frei = "W14a-gibt-es-nicht";
            Assert.False(new HeizkesselStammCtrl().Exists(frei));
            Assert.False(new SolarkollektorenStammCtrl().Exists(frei));
            Assert.False(new PufferSpStammCtrl().Exists(frei));
            Assert.False(new PhotovoltaikStammCtrl().Exists(frei));
            Assert.False(new StromspeicherStammCtrl().Exists(frei));
        }

        // =================================================================================
        // 6 - Die Satzzahlen der sechs Kataloge
        //
        // Der Anker, an dem jede spaetere Zeilenzaehlung haengt.
        // =================================================================================

        [Fact]
        public void Die_sechs_Kataloge_haben_die_eingefrorenen_Satzzahlen()
        {
            if (!_db.Vorhanden) return;

            var heiz = new HeizkesselStammCtrl(); heiz.ReadAll();
            var bhkw = new BHKWStammCtrl(); bhkw.ReadAll();
            var solar = new SolarkollektorenStammCtrl(); solar.ReadAll();
            var puffer = new PufferSpStammCtrl(); puffer.ReadAll();
            var pv = new PhotovoltaikStammCtrl(); pv.ReadAll();
            var speicher = new StromspeicherStammCtrl(); speicher.ReadAll();

            Assert.Equal(63, heiz.rows);
            Assert.Equal(79, bhkw.rows);
            Assert.Equal(7, solar.rows);
            Assert.Equal(13, puffer.rows);
            Assert.Equal(6, pv.rows);
            Assert.Equal(5, speicher.rows);
        }
    }
}
