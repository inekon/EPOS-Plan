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
    /// <para><b>Die Heizkesselzahlen standen bewusst zuerst VOR W14a.0b.</b> Der Kern bildete
    /// „Sonstige" auf <c>Brennstoff=23</c> ab, die Admin-Maske dagegen
    /// <c>Fernwärme=23</c>, <c>Sonstige Energieträger=24</c>, <c>Wasserstoff=25</c>
    /// (Befund W14-B2). <see cref="Heizkessel_Brennstoffgruppen_nach_der_Berichtigung"/>
    /// hielt zuerst den Zustand VORHER fest (drei Gruppen je 62) und steht seit W14a.0b auf
    /// den berichtigten Zahlen (je 0); die Gegenueberstellung steht im Portprotokoll.</para>
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
        /// Die dreizehn Brennstoffgruppen NACH der Berichtigung aus W14a.0b (Befund W14-B2).
        ///
        /// <para><b>Vorher / nachher</b>, auf <c>Kenndaten_Test.sqlite</c> gemessen: Zehn
        /// Gruppen sind unveraendert (Alle 62, Gas 52, Öl 3, Strom 8, die uebrigen sechs 0).
        /// DREI Gruppen aendern sich, weil sie in der alten Kette gar nicht standen und die
        /// Einengung deshalb aufhoben — <c>Fernwärme 62 → 0</c>,
        /// <c>Sonstige Energieträger 62 → 0</c>, <c>Wasserstoff 62 → 0</c>. Der Testkatalog
        /// fuehrt keinen Kessel mit Brennstoff 23, 24 oder 25; die Null ist also richtig und
        /// nicht etwa ein leerer Filter.</para>
        /// </summary>
        [Fact]
        public void Heizkessel_Brennstoffgruppen_nach_der_Berichtigung()
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

                // W14a.0b: bis dahin je 62 (keine Einengung - der ganze Katalog), jetzt
                // die richtigen Brennstoffnummern 23 / 24 / 25. Befund W14-B2.
                ["Fernwärme"] = 0,
                ["Sonstige Energieträger"] = 0,
                ["Wasserstoff"] = 0
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
        // 6 - KatalogBrowserProfil (W14a.0a) - ohne Datenbank
        // =================================================================================

        /// <summary>
        /// Die vier Auspraegungen sind vollstaendig und tragen die gemessene Feldzahl
        /// (8 / 8 / 8 / 6 aus der Vermessung § 1-3 und § 5).
        /// </summary>
        [Fact]
        public void Browserprofil_kennt_die_vier_Auspraegungen_mit_ihrer_Feldzahl()
        {
            var erwartet = new Dictionary<KatalogBrowserArt, int>
            {
                [KatalogBrowserArt.Heizkessel] = 8,
                [KatalogBrowserArt.Bhkw] = 8,
                [KatalogBrowserArt.Solarkollektoren] = 8,
                [KatalogBrowserArt.Pufferspeicher] = 6
            };

            Assert.Equal(4, KatalogBrowserProfil.AlleArten.Count());
            foreach (var art in KatalogBrowserProfil.AlleArten)
            {
                var profil = KatalogBrowserProfil.Finde(art);
                Assert.Equal(erwartet[art], profil.Detailfelder.Count);
                Assert.False(string.IsNullOrEmpty(profil.Stammtabelle));
                Assert.False(string.IsNullOrEmpty(profil.HilfeSchluessel));

                // Der Bezeichner steht in jeder Auspraegung an erster Stelle und ist nie
                // editierbar - er ist der Schluessel des UPDATE.
                Assert.Equal(KatalogBrowserProfil.FeldBezeichner, profil.Detailfelder[0].Schluessel);
                Assert.False(profil.Detailfelder[0].Editierbar);
            }
        }

        /// <summary>
        /// Der Speicherweg gibt es nur bei Heizkessel und BHKW, dort mit je SECHS
        /// editierbaren Feldern (Vermessung § 1 b und § 2 b).
        /// </summary>
        [Fact]
        public void Browserprofil_traegt_den_Speicherweg_nur_wo_es_ihn_gibt()
        {
            var heiz = KatalogBrowserProfil.Finde(KatalogBrowserArt.Heizkessel);
            var bhkw = KatalogBrowserProfil.Finde(KatalogBrowserArt.Bhkw);
            var solar = KatalogBrowserProfil.Finde(KatalogBrowserArt.Solarkollektoren);
            var puffer = KatalogBrowserProfil.Finde(KatalogBrowserArt.Pufferspeicher);

            Assert.True(heiz.HatSpeicherweg);
            Assert.True(bhkw.HatSpeicherweg);
            Assert.False(solar.HatSpeicherweg);
            Assert.False(puffer.HatSpeicherweg);

            Assert.Equal(6, heiz.Detailfelder.Count(f => f.Editierbar));
            Assert.Equal(6, bhkw.Detailfelder.Count(f => f.Editierbar));
            Assert.Equal(0, solar.Detailfelder.Count(f => f.Editierbar));
            Assert.Equal(0, puffer.Detailfelder.Count(f => f.Editierbar));
        }

        /// <summary>
        /// Filterart, Listendarstellung und Schreibschutzanzeige je Auspraegung —
        /// die Ausprägungstabelle der Vermessung § 12.1 als Probe.
        /// </summary>
        [Fact]
        public void Browserprofil_bildet_die_Auspraegungstabelle_ab()
        {
            var heiz = KatalogBrowserProfil.Finde(KatalogBrowserArt.Heizkessel);
            var bhkw = KatalogBrowserProfil.Finde(KatalogBrowserArt.Bhkw);
            var solar = KatalogBrowserProfil.Finde(KatalogBrowserArt.Solarkollektoren);
            var puffer = KatalogBrowserProfil.Finde(KatalogBrowserArt.Pufferspeicher);

            Assert.Equal(KatalogFilterArt.BrennstoffUndLeistung, heiz.Filterart);
            Assert.Equal(KatalogFilterArt.BrennstoffUndLeistung, bhkw.Filterart);
            Assert.Equal(KatalogFilterArt.Keiner, solar.Filterart);
            Assert.Equal(KatalogFilterArt.HerstellerUndVolumen, puffer.Filterart);

            Assert.False(heiz.Zweispaltig);
            Assert.True(bhkw.Zweispaltig);
            Assert.True(solar.Zweispaltig);
            Assert.False(puffer.Zweispaltig);

            // Nur das BHKW faerbt geschuetzte Saetze grau und fragt beim Ueberschreiben.
            Assert.True(bhkw.ZeigtSchreibschutz);
            Assert.False(heiz.ZeigtSchreibschutz);
            Assert.False(solar.ZeigtSchreibschutz);
            Assert.False(puffer.ZeigtSchreibschutz);

            // Der Zeilenbauplan gehoert zur zweiten Spalte - drei Teile beim BHKW,
            // zwei bei den Kollektoren, keiner bei den einspaltigen Listen.
            Assert.Equal(3, bhkw.Zeilenbauplan.Count);
            Assert.Equal(2, solar.Zeilenbauplan.Count);
            Assert.Empty(heiz.Zeilenbauplan);
            Assert.Empty(puffer.Zeilenbauplan);
        }

        /// <summary>
        /// Ohne Uebersetzer liefert das Profil die Schluessel selbst — so laesst es sich
        /// ohne Ressourcenkatalog pruefen (Muster <see cref="KatalogImportProfil"/>).
        /// </summary>
        [Fact]
        public void Browserprofil_ohne_Uebersetzer_liefert_die_Schluessel()
        {
            var profil = KatalogBrowserProfil.Finde(KatalogBrowserArt.Heizkessel);
            Assert.Equal("KBROW_TITEL_HEIZKESSEL", profil.Titel);
            Assert.Equal("KBROW_LBL_NAME", profil.Detailfelder[0].Bezeichnung);

            var uebersetzt = KatalogBrowserProfil.Finde(KatalogBrowserArt.Heizkessel,
                                                        s => s == "KBROW_TITEL_HEIZKESSEL" ? "Kessel" : s);
            Assert.Equal("Kessel", uebersetzt.Titel);
        }

        /// <summary>
        /// Der Feldname einer Pruefmeldung ist die Beschriftung ohne Doppelpunkt —
        /// genau die Regel <c>label.Text.TrimEnd(' ', ':')</c> der beiden Vorlaeufer.
        /// </summary>
        [Fact]
        public void Browserprofil_Feldname_ist_die_Beschriftung_ohne_Doppelpunkt()
        {
            var feld = new BrowserDetailfeld("X", "Thermische Leistung:", "kW", BrowserFeldArt.Zahl, true);
            Assert.Equal("Thermische Leistung", feld.Feldname);
        }

        // =================================================================================
        // 7 - Die Satzzahlen der sechs Kataloge
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

        // =================================================================================
        // 8 - KatalogZeilen und KatalogsatzAnzeige (W14a.0c)
        // =================================================================================

        /// <summary>
        /// Der Detailblock des Heizkesselbrowsers: acht Schluessel, die Zahlen mit
        /// <c>F2</c>, der Brennstoff als Nachschlag, <c>NULL</c> als leerer Text.
        /// </summary>
        [Fact]
        public void Heizkessel_Katalogsatz_zeigt_die_acht_Felder_wie_der_Bestand()
        {
            if (!_db.Vorhanden) return;
            using var _ = new DeutscheOberflaeche();

            var ctrl = new HeizkesselStammCtrl();
            var satz = ctrl.KatalogsatzAnzeige("GC7000F 22 23 - MX25");

            Assert.NotNull(satz);
            Assert.Equal(8, satz.Count);
            Assert.Equal("GC7000F 22 23 - MX25", satz[KatalogBrowserProfil.FeldBezeichner]);
            Assert.Equal("Brennwert-Kessel", satz[KatalogBrowserProfil.FeldBeschreibung]);
            Assert.Equal(ctrl.Brennstoffart[2], satz[KatalogBrowserProfil.FeldBrennstoff]);
            Assert.Equal("22,00", satz[KatalogBrowserProfil.FeldPtherm]);
            Assert.Equal("0,00", satz[KatalogBrowserProfil.FeldInvestitionskosten]);
            Assert.Equal("1", satz[KatalogBrowserProfil.FeldBrennwert]);

            // Vorlauf und Ruecklauf sind NULL - der Bestand zeigte dort einen leeren Text.
            Assert.Equal("", satz[KatalogBrowserProfil.FeldVorlauf]);
            Assert.Equal("", satz[KatalogBrowserProfil.FeldRuecklauf]);

            Assert.Null(ctrl.KatalogsatzAnzeige("W14a-gibt-es-nicht"));
        }

        /// <summary>
        /// Jedes Profilfeld findet einen Wert — kein Feld bleibt ohne Antwort, und kein
        /// Wert steht ohne Feld da. Das ist die Klammer zwischen Profil und Controller.
        /// </summary>
        [Fact]
        public void Jeder_Katalogsatz_beantwortet_genau_die_Felder_seines_Profils()
        {
            if (!_db.Vorhanden) return;

            Pruefe(KatalogBrowserArt.Heizkessel,
                   new HeizkesselStammCtrl().KatalogsatzAnzeige("GC7000F 22 23 - MX25"));
            Pruefe(KatalogBrowserArt.Bhkw,
                   BHKWStammCtrl.KatalogsatzAnzeige("2G 250kw.el Gas"));
            Pruefe(KatalogBrowserArt.Solarkollektoren,
                   SolarkollektorenStammCtrl.KatalogsatzAnzeige("SO4000TFV-FCC220-2V"));
            Pruefe(KatalogBrowserArt.Pufferspeicher,
                   PufferSpStammCtrl.KatalogsatzAnzeige("Puffer 3000Ltr"));

            static void Pruefe(KatalogBrowserArt art, IReadOnlyDictionary<string, string> satz)
            {
                Assert.NotNull(satz);
                var profil = KatalogBrowserProfil.Finde(art);
                Assert.Equal(profil.Detailfelder.Count, satz.Count);
                foreach (var feld in profil.Detailfelder)
                    Assert.True(satz.ContainsKey(feld.Schluessel),
                                art + ": Feld " + feld.Schluessel + " fehlt in der Anzeige.");
            }
        }

        /// <summary>
        /// Der BHKW-Detailblock zeigt seine Zahlen ROH, ohne Format — anders als der
        /// Heizkessel, der <c>F2</c> nimmt (Vermessung § 2 b gegen § 1 b).
        /// </summary>
        [Fact]
        public void Bhkw_Katalogsatz_zeigt_die_Zahlen_ohne_Format()
        {
            if (!_db.Vorhanden) return;
            using var _ = new DeutscheOberflaeche();

            var satz = BHKWStammCtrl.KatalogsatzAnzeige("2G 250kw.el Gas");
            Assert.NotNull(satz);
            Assert.Equal("2-G Energietechnik GmbH", satz[KatalogBrowserProfil.FeldFirma]);
            Assert.Equal("250", satz[KatalogBrowserProfil.FeldPtherm]);
            Assert.Equal("250", satz[KatalogBrowserProfil.FeldPel]);
            Assert.Equal("15", satz[KatalogBrowserProfil.FeldGrenzleistung]);
            Assert.Equal("85", satz[KatalogBrowserProfil.FeldVorlauf]);
            Assert.Equal("65", satz[KatalogBrowserProfil.FeldRuecklauf]);
        }

        /// <summary>
        /// Der Auslieferungskatalog des BHKW ist vollstaendig schreibgeschuetzt — die
        /// Rueckfrage beim Ueberschreiben ist dort der Regelfall
        /// (<c>Form_BHKWAdmin.cs:413-417</c>).
        /// </summary>
        [Fact]
        public void Bhkw_Auslieferungskatalog_ist_schreibgeschuetzt()
        {
            if (!_db.Vorhanden) return;

            Assert.True(BHKWStammCtrl.IstSchreibgeschuetzt("2G 250kw.el Gas"));
            Assert.False(BHKWStammCtrl.IstSchreibgeschuetzt("W14a-gibt-es-nicht"));
        }

        /// <summary>
        /// Die Kollektorliste: sieben Saetze, sortiert, mit den drei Werten der zweiten
        /// Rasterspalte.
        /// </summary>
        [Fact]
        public void Solarkollektoren_Katalogzeilen_tragen_die_Zweitspalte()
        {
            if (!_db.Vorhanden) return;

            var zeilen = SolarkollektorenStammCtrl.KatalogZeilen();
            Assert.Equal(7, zeilen.Count);

            var namen = zeilen.Select(z => z.Bezeichner).ToList();
            Assert.Equal(namen.OrderBy(n => n, StringComparer.Ordinal).ToList(), namen);

            var erste = zeilen.First(z => z.Bezeichner == "SO4000TFV-FCC220-2V");
            Assert.Equal("Junkers Bosch", erste.Firma);
            Assert.Equal("Flachkollektor", erste.Kollektortyp);
            Assert.Equal(1.94, erste.Aperturflaeche, 6);
        }

        /// <summary>
        /// Befund W14a-B78 / W14-B15: „Kollektorfläche" bleibt LEER, „Aperturfläche"
        /// traegt die Aperturflaeche. Woertlich wie der Bestand — Entscheide E-2 und E-11.
        /// </summary>
        [Fact]
        public void Solarkollektoren_Kollektorflaeche_bleibt_leer()
        {
            if (!_db.Vorhanden) return;
            using var _ = new DeutscheOberflaeche();

            var satz = SolarkollektorenStammCtrl.KatalogsatzAnzeige("SO4000TFV-FCC220-2V");
            Assert.NotNull(satz);
            Assert.Equal("", satz[KatalogBrowserProfil.FeldModulflaeche]);
            Assert.Equal("1,94", satz[KatalogBrowserProfil.FeldAperturflaeche]);
        }

        /// <summary>
        /// Der Pufferspeicher-Detailblock zeigt ROH, nicht mit einer Nachkommastelle wie
        /// <c>PufferSpStammCtrl.Detail</c> des PROJEKTdialogs — zwei Masken, zwei
        /// Anzeigeregeln, beide woertlich.
        /// </summary>
        [Fact]
        public void PufferSp_Katalogsatz_zeigt_roh_und_nicht_wie_der_Projektdialog()
        {
            if (!_db.Vorhanden) return;
            using var _ = new DeutscheOberflaeche();

            var satz = PufferSpStammCtrl.KatalogsatzAnzeige("Puffer 3000Ltr");
            Assert.NotNull(satz);
            Assert.Equal("Bosch", satz[KatalogBrowserProfil.FeldFirma]);
            Assert.Equal("Pufferspeicher", satz[KatalogBrowserProfil.FeldSpeichertyp]);
            Assert.Equal("3,34", satz[KatalogBrowserProfil.FeldVerluste]);
            Assert.Equal("3000", satz[KatalogBrowserProfil.FeldVolumen]);
            Assert.Equal("0", satz[KatalogBrowserProfil.FeldInvestitionskosten]);

            // Der Projektdialog formatiert dieselben Werte mit einer Nachkommastelle.
            var detail = PufferSpStammCtrl.Detail("Puffer 3000Ltr");
            Assert.Equal("3,3", detail.Bereitschaftsverluste);
        }

        // =================================================================================
        // 9 - SpeichertypAbbildung (W14a.0d)
        // =================================================================================

        /// <summary>
        /// Die drei DB-Werte und ihre Reihenfolge — sie sind Persistenz und duerfen sich
        /// nicht bewegen.
        /// </summary>
        [Fact]
        public void Speichertyp_DB_Werte_stehen_in_der_Reihenfolge_der_Auswahlliste()
        {
            Assert.Equal(new[] { "Solarspeicher", "Pufferspeicher", "Kombispeicher" },
                         PufferSpStammCtrl.SPEICHERTYP_DB_WERTE);
            Assert.Equal(DbWerte.PSP_SPEICHERTYP_SOLAR, PufferSpStammCtrl.SPEICHERTYP_DB_WERTE[0]);
            Assert.Equal(DbWerte.PSP_SPEICHERTYP_PUFFER, PufferSpStammCtrl.SPEICHERTYP_DB_WERTE[1]);
            Assert.Equal(DbWerte.PSP_SPEICHERTYP_KOMBI, PufferSpStammCtrl.SPEICHERTYP_DB_WERTE[2]);
        }

        /// <summary>
        /// Die drei EINGEFRORENEN englischen Altwerte (Befund L0-1). Sie beschreiben
        /// Altdaten, nicht die heutige Oberflaeche, und duerfen sich mit einer
        /// Uebersetzungskorrektur NICHT mitaendern.
        /// </summary>
        [Fact]
        public void Speichertyp_Altwerte_bleiben_eingefroren()
        {
            Assert.Equal(new[] { "Solar storage", "Buffer storage", "Combination storage" },
                         PufferSpStammCtrl.SPEICHERTYP_ALTWERTE_EN);
        }

        /// <summary>
        /// Der LESEWEG erkennt drei Stufen: DB-Wert, angezeigter Text, englischer Altwert.
        /// Alles andere ist <c>-1</c> und bleibt als Freitext stehen.
        /// </summary>
        [Fact]
        public void Speichertyp_Index_kennt_die_drei_Stufen()
        {
            var anzeige = new[] { "Solarspeicher", "Pufferspeicher", "Kombispeicher" };

            // Stufe 1 - der DB-Wert, auch mit abweichender Gross-/Kleinschreibung.
            Assert.Equal(0, PufferSpStammCtrl.SpeichertypIndex("Solarspeicher", anzeige));
            Assert.Equal(1, PufferSpStammCtrl.SpeichertypIndex("PUFFERSPEICHER", anzeige));

            // Stufe 2 - der angezeigte Text der aktuellen Sprache.
            Assert.Equal(2, PufferSpStammCtrl.SpeichertypIndex("Kombispeicher",
                                                               new[] { "a", "b", "Kombispeicher" }));

            // Stufe 3 - der englische Altwert (Bestandstoleranz).
            Assert.Equal(0, PufferSpStammCtrl.SpeichertypIndex("Solar storage", anzeige));
            Assert.Equal(1, PufferSpStammCtrl.SpeichertypIndex("Buffer storage", anzeige));
            Assert.Equal(2, PufferSpStammCtrl.SpeichertypIndex("Combination storage", anzeige));

            Assert.Equal(-1, PufferSpStammCtrl.SpeichertypIndex("Eiswürfelspeicher", anzeige));
            Assert.Equal(-1, PufferSpStammCtrl.SpeichertypIndex("", anzeige));
            Assert.Equal(-1, PufferSpStammCtrl.SpeichertypIndex(null, anzeige));
        }

        /// <summary>
        /// Der SCHREIBWEG: massgeblich ist der Index. Ohne Auswahl entscheidet der Text,
        /// und ein unbekannter Freitext geht unveraendert durch — er wird nicht
        /// stillschweigend umgeschrieben.
        /// </summary>
        [Fact]
        public void Speichertyp_DbWert_nimmt_den_Index_und_laesst_Freitext_stehen()
        {
            Assert.Equal("Solarspeicher", PufferSpStammCtrl.SpeichertypDbWert(0));
            Assert.Equal("Pufferspeicher", PufferSpStammCtrl.SpeichertypDbWert(1));
            Assert.Equal("Kombispeicher", PufferSpStammCtrl.SpeichertypDbWert(2));

            // Englischer Altwert ohne Auswahl -> der deutsche Persistenzwert.
            Assert.Equal("Pufferspeicher", PufferSpStammCtrl.SpeichertypDbWert(-1, "Buffer storage"));

            // Unbekannter Freitext bleibt stehen.
            Assert.Equal("Eiswürfelspeicher",
                         PufferSpStammCtrl.SpeichertypDbWert(-1, "Eiswürfelspeicher"));
            Assert.Equal("", PufferSpStammCtrl.SpeichertypDbWert(-1, ""));
        }

        /// <summary>
        /// Jeder Speichertyp der Testdatenbank wird vom Leseweg erkannt — sonst ginge der
        /// Wert beim naechsten Speichern verloren.
        /// </summary>
        [Fact]
        public void Speichertyp_jeder_Bestandswert_wird_erkannt()
        {
            if (!_db.Vorhanden) return;

            var ctrl = new PufferSpStammCtrl();
            ctrl.ReadAll();
            foreach (var m in ctrl.items)
                Assert.True(PufferSpStammCtrl.SpeichertypIndex(m.Speichertyp) >= 0,
                            "Speichertyp \"" + m.Speichertyp + "\" wird nicht erkannt.");
        }


        // =================================================================================
        // 10 - SpeichernAus / Anlegen / Ueberschreiben / Loeschen (W14a.0e)
        //
        // Alle Faelle enden VOR dem Schreiben: Sie pruefen die Ablehnungsgruende, nicht
        // den Schreibvorgang selbst. Die Arbeitskopie bleibt damit unberuehrt und kann
        // von der ganzen Klasse geteilt werden (Regel seit W11a).
        // =================================================================================

        /// <summary>
        /// „Speichern unter" auf einen vergebenen Namen wird abgelehnt — der
        /// <c>Exists</c>-Vorabtest von <c>Form_PufferSp_Bearbeiten</c> (Z. 226, 297).
        /// </summary>
        [Fact]
        public void PufferSp_Anlegen_lehnt_einen_vergebenen_Namen_ab()
        {
            if (!_db.Vorhanden) return;
            using var _ = new DeutscheOberflaeche();

            var daten = new PufferSpModel { Firma = "W14a", Speichertyp = "Pufferspeicher" };
            var ergebnis = PufferSpStammCtrl.Anlegen(daten, "Puffer 3000Ltr");

            Assert.False(ergebnis.Ok);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.PSP_MELDUNG_NAME_EXISTIERT, ergebnis.Meldung);
            Assert.Equal("", ergebnis.Name);
        }

        /// <summary>Ohne Namen geht gar nichts — und die Meldung sagt das auch.</summary>
        [Fact]
        public void PufferSp_Anlegen_und_Ueberschreiben_verlangen_einen_Bezeichner()
        {
            using var _ = new DeutscheOberflaeche();

            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.PSP_MELDUNG_BEZEICHNER_UNGUELTIG,
                         PufferSpStammCtrl.Anlegen(new PufferSpModel(), "  ").Meldung);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.PSP_MELDUNG_BEZEICHNER_UNGUELTIG,
                         PufferSpStammCtrl.Anlegen(null, "X").Meldung);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.PSP_MELDUNG_BEZEICHNER_UNGUELTIG,
                         PufferSpStammCtrl.Ueberschreiben(new PufferSpModel { Name = "" }).Meldung);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.PSP_MELDUNG_BEZEICHNER_UNGUELTIG,
                         PufferSpStammCtrl.Loeschen("").Meldung);
        }

        /// <summary>
        /// Befund W14-B33: Der Modulkatalog der Photovoltaik lehnt einen vergebenen Namen
        /// ab — und meldet den Grund, statt zu schweigen.
        /// </summary>
        [Fact]
        public void Photovoltaik_SpeichernAus_lehnt_einen_vergebenen_Namen_ab()
        {
            if (!_db.Vorhanden) return;
            using var _ = new DeutscheOberflaeche();

            var daten = new PhotovoltaikModel { m_szName = "Ablytek 6MN6A270" };
            var ergebnis = PhotovoltaikStammCtrl.SpeichernAus(daten, neu: true, schluessel: null);

            Assert.False(ergebnis.Ok);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.PSP_MELDUNG_NAME_EXISTIERT, ergebnis.Meldung);
        }

        /// <summary>
        /// Befund W14-B47: Der Stromspeicher bekommt seinen <c>Exists</c>-Vorabtest —
        /// bis hierher legte er ohne Vorabtest an.
        /// </summary>
        [Fact]
        public void Stromspeicher_SpeichernAus_lehnt_einen_vergebenen_Namen_ab()
        {
            if (!_db.Vorhanden) return;
            using var _ = new DeutscheOberflaeche();

            var daten = new StromspeicherModel { m_szBezeichner = "BYD HVS+ 12.8" };
            var ergebnis = StromspeicherStammCtrl.SpeichernAus(daten, neu: true, schluessel: null);

            Assert.False(ergebnis.Ok);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.PSP_MELDUNG_NAME_EXISTIERT, ergebnis.Meldung);
        }

        /// <summary>Ein leerer Bezeichner wird in beiden Modulkatalogen abgelehnt.</summary>
        [Fact]
        public void Modulkataloge_verlangen_einen_Bezeichner()
        {
            using var _ = new DeutscheOberflaeche();

            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.PSP_MELDUNG_BEZEICHNER_UNGUELTIG,
                         PhotovoltaikStammCtrl.SpeichernAus(new PhotovoltaikModel(), true, null).Meldung);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.PSP_MELDUNG_BEZEICHNER_UNGUELTIG,
                         StromspeicherStammCtrl.SpeichernAus(new StromspeicherModel(), true, null).Meldung);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.PSP_MELDUNG_BEZEICHNER_UNGUELTIG,
                         PhotovoltaikStammCtrl.Loeschen(null).Meldung);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.PSP_MELDUNG_BEZEICHNER_UNGUELTIG,
                         StromspeicherStammCtrl.Loeschen(" ").Meldung);
        }

        /// <summary>
        /// Der BHKW-Speicherweg lehnt einen geschuetzten Satz ab, SOLANGE die Rueckfrage
        /// nicht bejaht ist — und schreibt dabei nichts. Genau die Regel von
        /// <c>Form_BHKWAdmin.cs:418-430</c>.
        /// </summary>
        [Fact]
        public void Bhkw_Speicherweg_haelt_am_Schreibschutz_an()
        {
            if (!_db.Vorhanden) return;
            using var _ = new DeutscheOberflaeche();

            var vorher = BHKWStammCtrl.KatalogsatzAnzeige("2G 250kw.el Gas");
            var felder = new BHKWStammCtrl.AnzeigefelderBhkw("W14a-Probe", 1, 2, 3, 4, 5);

            var ergebnis = BHKWStammCtrl.AnzeigefelderSchreiben("2G 250kw.el Gas", felder,
                                                                schreibschutzUebergehen: false);
            Assert.False(ergebnis.Ok);
            Assert.False(string.IsNullOrEmpty(ergebnis.Meldung));

            // Und der Satz steht unveraendert da.
            var nachher = BHKWStammCtrl.KatalogsatzAnzeige("2G 250kw.el Gas");
            Assert.Equal(vorher[KatalogBrowserProfil.FeldFirma], nachher[KatalogBrowserProfil.FeldFirma]);
        }

        /// <summary>
        /// Der Heizkessel-Speicherweg bricht bei einem unbekannten Bezeichner ab, ohne zu
        /// schreiben.
        /// </summary>
        [Fact]
        public void Heizkessel_Speicherweg_bricht_ohne_Satz_ab()
        {
            if (!_db.Vorhanden) return;
            using var _ = new DeutscheOberflaeche();

            var felder = new HeizkesselStammCtrl.AnzeigefelderHeizkessel("x", 1, 2, true, 3, 4);
            var ergebnis = HeizkesselStammCtrl.AnzeigefelderSchreiben("W14a-gibt-es-nicht", felder);

            Assert.False(ergebnis.Ok);
            Assert.False(string.IsNullOrEmpty(ergebnis.Meldung));
            Assert.Equal("", ergebnis.Name);
        }

        // =================================================================================
        // 11 - ModulKatalogProfil (W14a.0a) und die zwei Vorgabewerte (W14a.0f)
        // =================================================================================

        /// <summary>
        /// Beide Auspraegungen fuehren dreizehn Felder; nur der Stromspeicher hat eine
        /// zweite Feldgruppe (AP3-Gerätetechnik).
        /// </summary>
        [Fact]
        public void Modulkatalogprofil_kennt_zwei_Auspraegungen_mit_je_dreizehn_Feldern()
        {
            Assert.Equal(2, ModulKatalogProfil.AlleArten.Count());

            var sp = ModulKatalogProfil.Finde(ModulKatalogArt.Stromspeicher);
            var pv = ModulKatalogProfil.Finde(ModulKatalogArt.Photovoltaik);

            Assert.Equal(13, sp.Felder.Count);
            Assert.Equal(13, pv.Felder.Count);

            Assert.Equal(7, sp.Felder.Count(f => f.Gruppe == 0));
            Assert.Equal(6, sp.Felder.Count(f => f.Gruppe == 1));
            Assert.Equal(13, pv.Felder.Count(f => f.Gruppe == 0));
            Assert.Equal("", pv.GruppeZwei);

            // Der Bezeichner ist in beiden der Schluessel und deshalb gesperrt.
            Assert.Equal(ModulKatalogProfil.FeldBezeichner, sp.Felder[0].Schluessel);
            Assert.Equal(ModulKatalogProfil.FeldBezeichner, pv.Felder[0].Schluessel);
            Assert.True(sp.Felder[0].Gesperrt);
            Assert.True(pv.Felder[0].Gesperrt);
        }

        /// <summary>
        /// <b>BITGLEICH:</b> die <c>leerErlaubt</c>-Regel je Feld. Photovoltaik: NEUN von
        /// zehn Zahlfeldern duerfen leer sein, die Nennleistung nicht. Stromspeicher:
        /// KEINES der fuenf Bestandsfelder, ALLE SECHS AP3-Felder.
        /// </summary>
        [Fact]
        public void Modulkatalogprofil_haelt_die_leerErlaubt_Regel_je_Feld()
        {
            var pv = ModulKatalogProfil.Finde(ModulKatalogArt.Photovoltaik);
            var zahlen = pv.Felder.Where(f => f.Art != BrowserFeldArt.Text &&
                                              f.Art != BrowserFeldArt.Mehrzeilig).ToList();
            Assert.Equal(10, zahlen.Count);
            Assert.Equal(9, zahlen.Count(f => f.LeerErlaubt));
            Assert.False(pv.Felder.First(f => f.Schluessel == ModulKatalogProfil.FeldLeistung).LeerErlaubt);

            var sp = ModulKatalogProfil.Finde(ModulKatalogArt.Stromspeicher);
            var bestand = sp.Felder.Where(f => f.Gruppe == 0 && f.Art != BrowserFeldArt.Text).ToList();
            Assert.Equal(5, bestand.Count);
            Assert.All(bestand, f => Assert.False(f.LeerErlaubt));
            Assert.All(sp.Felder.Where(f => f.Gruppe == 1), f => Assert.True(f.LeerErlaubt));

            // Der Typ ist ein TEXTfeld und darf ebenfalls nicht leer sein
            // ("Eingaben ueberpruefen!", Form_AdminStromspeicher.cs:99-103).
            Assert.False(sp.Felder.First(f => f.Schluessel == ModulKatalogProfil.FeldTyp).LeerErlaubt);
        }

        /// <summary>
        /// Die Vorbelegungen nach „Neu…" — dreizehn beim Stromspeicher (mit den zwei
        /// fachlichen Vorgaben), dreizehn bei der Photovoltaik (zwei leer, zehn Nullen).
        /// </summary>
        [Fact]
        public void Modulkatalogprofil_traegt_die_Vorbelegungen_von_Neu()
        {
            using var _ = new DeutscheOberflaeche();

            var sp = ModulKatalogProfil.Finde(ModulKatalogArt.Stromspeicher);
            Assert.Equal("", Vorgabe(sp, ModulKatalogProfil.FeldBezeichner));
            Assert.Equal("Lithium-Ionen", Vorgabe(sp, ModulKatalogProfil.FeldTyp));
            Assert.Equal("0,9", Vorgabe(sp, ModulKatalogProfil.FeldWirkungsgradRt));
            Assert.Equal("0,025", Vorgabe(sp, ModulKatalogProfil.FeldVerschleisskosten));
            Assert.Equal("0", Vorgabe(sp, ModulKatalogProfil.FeldEnergie));

            var pv = ModulKatalogProfil.Finde(ModulKatalogArt.Photovoltaik);
            Assert.Equal("", Vorgabe(pv, ModulKatalogProfil.FeldFirma));
            Assert.Equal("", Vorgabe(pv, ModulKatalogProfil.FeldBeschreibung));
            Assert.Equal(10, pv.Felder.Count(f => f.Vorgabe == "0"));

            static string Vorgabe(ModulKatalogProfil p, string schluessel) =>
                p.Felder.First(f => f.Schluessel == schluessel).Vorgabe;
        }

        /// <summary>
        /// W14a.0f: die zwei fachlichen Vorgaben stehen jetzt beieinander, und der
        /// Persistenzwert der Zelltechnologie in <see cref="DbWerte"/>. Werte
        /// unveraendert.
        /// </summary>
        [Fact]
        public void Die_zwei_Vorgabewerte_und_der_Persistenzwert_stehen_im_Kern()
        {
            Assert.Equal(0.90, StromspeicherModel.WIRKUNGSGRAD_RT_VORGABE, 9);
            Assert.Equal(0.025, StromspeicherModel.C_VER_VORGABE, 9);
            Assert.Equal("Lithium-Ionen", DbWerte.SP_TYP_LITHIUM_IONEN);
        }

        /// <summary>
        /// Der Detailblock des Speicherkatalogs beantwortet jedes Profilfeld — auch die
        /// sechs AP3-Spalten, die auf einer aelteren Datenbank fehlen koennen.
        /// </summary>
        [Fact]
        public void Stromspeicher_Katalogsatz_beantwortet_alle_dreizehn_Felder()
        {
            if (!_db.Vorhanden) return;
            using var _ = new DeutscheOberflaeche();

            var satz = StromspeicherStammCtrl.KatalogsatzAnzeige("BYD HVS+ 12.8");
            Assert.NotNull(satz);

            var profil = ModulKatalogProfil.Finde(ModulKatalogArt.Stromspeicher);
            Assert.Equal(profil.Felder.Count, satz.Count);
            foreach (var feld in profil.Felder)
                Assert.True(satz.ContainsKey(feld.Schluessel), "Feld " + feld.Schluessel + " fehlt.");

            Assert.Equal("BYD HVS+ 12.8", satz[ModulKatalogProfil.FeldBezeichner]);
            Assert.Equal("Lithium-Ionen-Akkus", satz[ModulKatalogProfil.FeldTyp]);
            Assert.Equal("12,8", satz[ModulKatalogProfil.FeldEnergie]);

            // Ein Satz ohne gepflegte AP3-Werte zeigt dort leere Felder statt einer 0.
            var ohneAp3 = StromspeicherStammCtrl.KatalogsatzAnzeige("VARTA element backup");
            Assert.Equal("", ohneAp3[ModulKatalogProfil.FeldWirkungsgradRt]);
            Assert.Equal("", ohneAp3[ModulKatalogProfil.FeldZyklen]);
        }

        /// <summary>Die Speicherliste kommt sortiert — fuenf Saetze.</summary>
        [Fact]
        public void Stromspeicher_Katalogzeilen_kommen_sortiert()
        {
            if (!_db.Vorhanden) return;

            var zeilen = StromspeicherStammCtrl.KatalogZeilen();
            Assert.Equal(5, zeilen.Count);

            var namen = zeilen.Select(z => z.Bezeichner).ToList();
            Assert.Equal(namen.OrderBy(n => n, StringComparer.Ordinal).ToList(), namen);
            Assert.All(zeilen, z => Assert.True(z.Id > 0));
        }


        // =================================================================================
        // 12 - Textkatalog W14a (W14a.0g) - jeder Schluessel in BEIDEN Sprachen
        // =================================================================================

        /// <summary>
        /// Jeder Beschriftungsschluessel der beiden Profile hat einen deutschen UND einen
        /// englischen Text.
        /// </summary>
        /// <remarks>
        /// <para>Zwei der sieben Masken waren gar nicht lokalisiert
        /// (<c>Form_BHKWAdmin</c> mit 26 deutschen Literalen, Befund W14-B11) und eine
        /// hatte genau EINEN englischen Text bei 29 (<c>Form_AdminPV</c>, Befund
        /// W14-B37). Diese Probe haelt fest, dass das nicht wiederkehrt: Sie faehrt beide
        /// Profile mit dem echten Ressourcenkatalog und laesst keinen Schluessel
        /// unaufgeloest.</para>
        /// <para>Sie ersetzt keine Uebersetzung, sie prueft ihre ANWESENHEIT — die
        /// Pruefrezeptur <c>Allgemein/Simulation/Lokalisierung_Pruefung.md</c> bleibt
        /// daneben bestehen.</para>
        /// </remarks>
        [Theory]
        [InlineData("de-DE")]
        [InlineData("en-US")]
        public void Jeder_Textschluessel_der_Welle_ist_in_beiden_Sprachen_da(string sprache)
        {
            var kultur = new System.Globalization.CultureInfo(sprache);
            var fehlend = new List<string>();

            string Uebersetzen(string schluessel)
            {
                string t = WindowsFormsApplication1.MyResource.Resource
                               .ResourceManager.GetString(schluessel, kultur);
                if (string.IsNullOrEmpty(t)) fehlend.Add(schluessel);
                return t ?? schluessel;
            }

            foreach (var art in KatalogBrowserProfil.AlleArten)
            {
                var p = KatalogBrowserProfil.Finde(art, Uebersetzen);
                Assert.False(string.IsNullOrEmpty(p.Titel));
                Assert.False(string.IsNullOrEmpty(p.Listenbeschriftung));
                Assert.False(string.IsNullOrEmpty(p.Detailueberschrift));
                Assert.All(p.Detailfelder, f => Assert.False(string.IsNullOrEmpty(f.Bezeichnung)));
            }

            foreach (var art in ModulKatalogProfil.AlleArten)
            {
                var p = ModulKatalogProfil.Finde(art, Uebersetzen);
                Assert.False(string.IsNullOrEmpty(p.Titel));
                Assert.False(string.IsNullOrEmpty(p.Listenbeschriftung));
                Assert.All(p.Felder, f => Assert.False(string.IsNullOrEmpty(f.Bezeichnung)));
            }

            Assert.True(fehlend.Count == 0,
                        sprache + ": " + string.Join(", ", fehlend.Distinct()));
        }

        /// <summary>
        /// Die Texte des Pufferspeicher-Editors und die drei Speichertypen der
        /// Auswahlliste — ebenfalls in beiden Sprachen.
        /// </summary>
        [Theory]
        [InlineData("de-DE")]
        [InlineData("en-US")]
        public void Die_Texte_des_Katalogeditors_sind_in_beiden_Sprachen_da(string sprache)
        {
            var kultur = new System.Globalization.CultureInfo(sprache);
            string[] schluessel =
            {
                "PSPK_TITEL", "PSPK_GRP_BEZEICHNUNG", "PSPK_LBL_NAME", "PSPK_LBL_HERSTELLER",
                "PSPK_LBL_SPEICHERTYP", "PSPK_GRP_TECHNIK", "PSPK_LBL_VERLUSTE",
                "PSPK_LBL_VOLUMEN", "PSPK_GRP_KOSTEN", "PSPK_LBL_INVEST",
                "PSPK_FELD_VOLUMEN", "PSPK_FELD_VERLUSTE", "PSPK_FELD_INVEST",
                "PSPK_TYP_SOLAR", "PSPK_TYP_PUFFER", "PSPK_TYP_KOMBI", "PSPK_MSG_SCHUTZ",
                "BHKWK_MSG_SCHUTZ", "MODK_MSG_SCHUTZ", "MODK_MSG_TYP_FEHLT",
                "KBROW_BTN_NEU", "KBROW_BTN_BEARBEITEN", "KBROW_BTN_LOESCHEN",
                "KBROW_MSG_AUSWAHL_BHKW", "KBROW_MSG_AUSWAHL_KOLLEKTOR",
                "KBROW_MSG_SCHUTZ_LOESCHEN", "KBROW_MSG_LOESCHEN_FEHLER", "KBROW_TITEL_SCHUTZ",
                "KBROW_SPALTE_NAME", "KBROW_SPALTE_EIGENSCHAFTEN"
            };

            foreach (string s in schluessel)
                Assert.False(string.IsNullOrEmpty(
                    WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(s, kultur)),
                    sprache + ": " + s + " fehlt.");
        }

        /// <summary>
        /// <b>Befund W14-B24 behoben.</b> Der englische Text des Namensfeldes im
        /// Pufferspeicher-Editor lautete „Boiler name:" — der Speichername, beschriftet
        /// als Kessel. Er heisst jetzt „Storage name:".
        /// </summary>
        [Fact]
        public void Der_Pufferspeicher_Editor_nennt_seinen_Namen_nicht_mehr_Kessel()
        {
            var en = new System.Globalization.CultureInfo("en-US");
            string text = WindowsFormsApplication1.MyResource.Resource
                              .ResourceManager.GetString("PSPK_LBL_NAME", en);

            Assert.Equal("Storage name:", text);
            Assert.DoesNotContain("Boiler", text, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Die drei Anzeigetexte der Speichertyp-Auswahl sind auf DEUTSCH genau die drei
        /// Persistenzwerte — deshalb trifft der Leseweg auch ohne Anzeigeliste.
        /// </summary>
        [Fact]
        public void Die_Speichertyp_Anzeigetexte_stimmen_auf_deutsch_mit_den_DB_Werten_ueberein()
        {
            var de = new System.Globalization.CultureInfo("de-DE");
            var rm = WindowsFormsApplication1.MyResource.Resource.ResourceManager;

            Assert.Equal(PufferSpStammCtrl.SPEICHERTYP_DB_WERTE[0], rm.GetString("PSPK_TYP_SOLAR", de));
            Assert.Equal(PufferSpStammCtrl.SPEICHERTYP_DB_WERTE[1], rm.GetString("PSPK_TYP_PUFFER", de));
            Assert.Equal(PufferSpStammCtrl.SPEICHERTYP_DB_WERTE[2], rm.GetString("PSPK_TYP_KOMBI", de));

            // Und auf ENGLISCH genau die drei eingefrorenen Altwerte - der Grund, warum
            // sie ueberhaupt in der Datenbank landen konnten (Befund L0-1).
            var en = new System.Globalization.CultureInfo("en-US");
            Assert.Equal(PufferSpStammCtrl.SPEICHERTYP_ALTWERTE_EN[0], rm.GetString("PSPK_TYP_SOLAR", en));
            Assert.Equal(PufferSpStammCtrl.SPEICHERTYP_ALTWERTE_EN[1], rm.GetString("PSPK_TYP_PUFFER", en));
            Assert.Equal(PufferSpStammCtrl.SPEICHERTYP_ALTWERTE_EN[2], rm.GetString("PSPK_TYP_KOMBI", en));
        }

        /// <summary>
        /// Pinnt Kultur und Oberflaechensprache auf de-DE — die Zahlenformate <c>F2</c>
        /// und die Ressourcentexte haengen daran (Regel seit iU9-W8).
        /// </summary>
        private sealed class DeutscheOberflaeche : IDisposable
        {
            private readonly System.Globalization.CultureInfo _kultur =
                System.Threading.Thread.CurrentThread.CurrentCulture;
            private readonly System.Globalization.CultureInfo _sprache =
                System.Threading.Thread.CurrentThread.CurrentUICulture;

            public DeutscheOberflaeche()
            {
                var de = new System.Globalization.CultureInfo("de-DE");
                System.Threading.Thread.CurrentThread.CurrentCulture = de;
                System.Threading.Thread.CurrentThread.CurrentUICulture = de;
            }

            public void Dispose()
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = _kultur;
                System.Threading.Thread.CurrentThread.CurrentUICulture = _sprache;
            }
        }
    }
}
