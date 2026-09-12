using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die Filterlogik des Waermepumpen-Katalogs (iU9-W7.0b).
    ///
    /// <para>Bis Welle 7 stand sie als LINQ-Ausdruck in
    /// <c>Form_WpFilterAuswahl.ApplyFilter</c> und liess sich nur am Windows-Geraet
    /// pruefen. Sie braucht keine Datenbank — deshalb rechnen diese Faelle auf einer
    /// von Hand gebauten Liste. Nur <see cref="KatalogZeilenTests"/> weiter unten
    /// greift auf die Testdatenbank zu.</para>
    /// </summary>
    public class WaermepumpenKatalogFilterTests
    {
        /// <summary>
        /// Sechs Zeilen, die jedes Merkmal mindestens zweimal belegen — sonst traefe
        /// ein Gleichheitsfilter immer alles und die Probe saehe nichts.
        /// </summary>
        private static IReadOnlyList<WaermepumpenKatalogZeile> Proben()
        {
            return new List<WaermepumpenKatalogZeile>
            {
                //                      Hersteller Bezeichnung Bauart      Aufstellung  MaxVL MinVL MaxLstg Zuheiz Prinzip         Regelung     Auslegung
                new WaermepumpenKatalogZeile("Alpha", "CS-070",  "Monoblock", "Innen",     55,   35,   7.0,   3,   "Luft-Wasser",  "stetig",    WaermepumpenKatalogZeile.AUSLEGUNG_HEIZEN),
                new WaermepumpenKatalogZeile("Alpha", "CS-127",  "Split",     "Außen",     60,   35,  12.7,   6,   "Luft-Wasser",  "einstufig", WaermepumpenKatalogZeile.AUSLEGUNG_HEIZEN_KUEHLEN),
                new WaermepumpenKatalogZeile("Beta",  "BX-200",  "Monoblock", "Innen",     45,   30,  20.0,   9,   "Sole-Wasser",  "zweistufig",WaermepumpenKatalogZeile.AUSLEGUNG_HEIZEN),
                new WaermepumpenKatalogZeile("Beta",  "BX-350",  "Split",     "Außen",     70,   45,  35.0,   9,   "Wasser-Wasser","stetig",    WaermepumpenKatalogZeile.AUSLEGUNG_HEIZEN_KUEHLEN),
                new WaermepumpenKatalogZeile("Gamma", "cs-990",  "Monoblock", "Außen",     35,   25,  99.0,   0,   "Sole-Wasser",  "stetig",    WaermepumpenKatalogZeile.AUSLEGUNG_HEIZEN),
                // Ohne Kennlinien: beide Vorlaeufe 0 - der Satz faellt aus jedem
                // Bereichsfilter mit Untergrenze > 0 heraus (Bestandsverhalten).
                new WaermepumpenKatalogZeile("Gamma", "ohne",    "",          "",           0,    0,   0.0,   0,   "",             "",          WaermepumpenKatalogZeile.AUSLEGUNG_HEIZEN)
            };
        }

        /// <summary>Alle Grenzen weit offen, keine Klappliste gesetzt: nichts faellt heraus.</summary>
        private static WaermepumpenKatalogFilter.Kriterien Offen()
            => new WaermepumpenKatalogFilter.Kriterien(VorlaufMax: 1000, LeistungMax: 1000);

        [Fact]
        public void Ohne_Kriterien_bleibt_der_ganze_Katalog_stehen()
        {
            var zeilen = Proben();
            var treffer = WaermepumpenKatalogFilter.Anwenden(zeilen, Offen());
            Assert.Equal(zeilen.Count, treffer.Count);
        }

        [Fact]
        public void Jeder_der_sieben_Gleichheitsfilter_greift()
        {
            var z = Proben();

            Assert.Equal(2, WaermepumpenKatalogFilter.Anwenden(z, Offen() with { Hersteller = "Alpha" }).Count);
            Assert.Equal(2, WaermepumpenKatalogFilter.Anwenden(z, Offen() with { Auslegung = WaermepumpenKatalogZeile.AUSLEGUNG_HEIZEN_KUEHLEN }).Count);
            Assert.Equal(2, WaermepumpenKatalogFilter.Anwenden(z, Offen() with { Funktionsprinzip = "Luft-Wasser" }).Count);
            Assert.Equal(3, WaermepumpenKatalogFilter.Anwenden(z, Offen() with { Regelung = "stetig" }).Count);
            Assert.Equal(3, WaermepumpenKatalogFilter.Anwenden(z, Offen() with { Bauart = "Monoblock" }).Count);
            Assert.Equal(3, WaermepumpenKatalogFilter.Anwenden(z, Offen() with { Aufstellung = "Außen" }).Count);
            Assert.Equal(2, WaermepumpenKatalogFilter.Anwenden(z, Offen() with { Zuheizung = "9" }).Count);
        }

        [Fact]
        public void Mehrere_Kriterien_wirken_zusammen()
        {
            var treffer = WaermepumpenKatalogFilter.Anwenden(Proben(),
                Offen() with { Hersteller = "Alpha", Bauart = "Split" });

            Assert.Single(treffer);
            Assert.Equal("CS-127", treffer[0].Bezeichnung);
        }

        [Fact]
        public void Die_beiden_Bereichsfilter_pruefen_MaxVorlauf_und_MaxLeistung()
        {
            var z = Proben();

            // Vorlauf 50…65 trifft CS-070 (55) und CS-127 (60).
            var nachVorlauf = WaermepumpenKatalogFilter.Anwenden(z,
                Offen() with { VorlaufMin = 50, VorlaufMax = 65 });
            Assert.Equal(new[] { "CS-070", "CS-127" }, nachVorlauf.Select(t => t.Bezeichnung));

            // Leistung 10…40 trifft CS-127 (12,7), BX-200 (20) und BX-350 (35).
            var nachLeistung = WaermepumpenKatalogFilter.Anwenden(z,
                Offen() with { LeistungMin = 10, LeistungMax = 40 });
            Assert.Equal(new[] { "CS-127", "BX-200", "BX-350" }, nachLeistung.Select(t => t.Bezeichnung));
        }

        [Fact]
        public void Die_Grenzen_gehoeren_dazu()
        {
            // Der Vorlaeufer vergleicht mit >= und <=; genau 55 muss CS-070 treffen.
            var treffer = WaermepumpenKatalogFilter.Anwenden(Proben(),
                Offen() with { VorlaufMin = 55, VorlaufMax = 55 });
            Assert.Single(treffer);
            Assert.Equal("CS-070", treffer[0].Bezeichnung);
        }

        [Fact]
        public void Ein_Satz_ohne_Kennlinien_faellt_aus_jedem_Vorlaufbereich_heraus()
        {
            var treffer = WaermepumpenKatalogFilter.Anwenden(Proben(),
                Offen() with { VorlaufMin = 1 });
            Assert.DoesNotContain(treffer, t => t.Bezeichnung == "ohne");

            // Mit Untergrenze 0 ist er wieder dabei - das ist die Vorbelegung.
            Assert.Contains(WaermepumpenKatalogFilter.Anwenden(Proben(), Offen()),
                            t => t.Bezeichnung == "ohne");
        }

        [Fact]
        public void Klartext_sucht_wie_Contains_und_ohne_Ruecksicht_auf_Gross_und_Klein()
        {
            var treffer = WaermepumpenKatalogFilter.Anwenden(Proben(), Offen() with { Suche = "cs" });
            Assert.Equal(new[] { "CS-070", "CS-127", "cs-990" }, treffer.Select(t => t.Bezeichnung));
        }

        [Fact]
        public void Der_Stern_steht_fuer_beliebig_viele_Zeichen()
        {
            // Das Beispiel aus der Beschriftung der Maske: "CS*7*".
            var treffer = WaermepumpenKatalogFilter.Anwenden(Proben(), Offen() with { Suche = "CS*7*" });
            Assert.Equal(new[] { "CS-070", "CS-127" }, treffer.Select(t => t.Bezeichnung));
        }

        [Fact]
        public void Das_Fragezeichen_steht_fuer_genau_ein_Zeichen()
        {
            Assert.Equal(new[] { "CS-070" },
                WaermepumpenKatalogFilter.Anwenden(Proben(), Offen() with { Suche = "CS-0?0" })
                                         .Select(t => t.Bezeichnung));

            // Ein Zeichen zu wenig trifft nicht - der Ausdruck ist verankert.
            Assert.Empty(WaermepumpenKatalogFilter.Anwenden(Proben(), Offen() with { Suche = "CS-0?" }));
        }

        [Fact]
        public void Ein_einzelner_Stern_und_ein_leeres_Feld_filtern_nicht()
        {
            Assert.Equal(6, WaermepumpenKatalogFilter.Anwenden(Proben(), Offen() with { Suche = "*" }).Count);
            Assert.Equal(6, WaermepumpenKatalogFilter.Anwenden(Proben(), Offen() with { Suche = "   " }).Count);
        }

        [Fact]
        public void Sonderzeichen_werden_woertlich_gesucht_statt_den_Ausdruck_zu_sprengen()
        {
            // BEFUND W7-O-1 (Bestandsverhalten, festgehalten statt repariert):
            // Der Auffangzweig "ungueltiges Muster = kein Filter" ist NICHT
            // erreichbar. Regex.Escape maskiert jedes Sonderzeichen, und die beiden
            // Ersetzungen danach machen aus "\*" und "\?" wieder gueltige Bausteine -
            // was hier ankommt, ist immer ein uebersetzbarer Ausdruck. Eine offene
            // Klammer wird deshalb WOERTLICH gesucht und trifft nichts, statt den
            // Filter abzuschalten. Diese Probe haelt fest, dass der Port daran nichts
            // geaendert hat.
            Assert.Empty(WaermepumpenKatalogFilter.Anwenden(Proben(), Offen() with { Suche = "[" }));

            // Und ein Bindestrich - in einer Zeichenklasse ein Bereichszeichen - wird
            // ebenfalls woertlich genommen und trifft die fuenf Modelle mit Bindestrich.
            Assert.Equal(5, WaermepumpenKatalogFilter.Anwenden(Proben(), Offen() with { Suche = "-" }).Count);
        }

        [Fact]
        public void Werte_liefert_je_Merkmal_eine_sortierte_Liste_ohne_Leerwerte()
        {
            var z = Proben();

            Assert.Equal(new[] { "Alpha", "Beta", "Gamma" },
                         WaermepumpenKatalogFilter.Werte(z, x => x.Hersteller));

            // Die leere Bauart des Satzes "ohne" faellt heraus.
            Assert.Equal(new[] { "Monoblock", "Split" },
                         WaermepumpenKatalogFilter.Werte(z, x => x.Bauart));

            Assert.Equal(new[] { "einstufig", "stetig", "zweistufig" },
                         WaermepumpenKatalogFilter.Werte(z, x => x.Regelung));
        }

        [Fact]
        public void Werte_traegt_kein_Alle_bei_sich()
        {
            // "Alle" ist Anzeigetext der Oberflaeche, nicht Inhalt der Liste (A-2).
            Assert.DoesNotContain("Alle", WaermepumpenKatalogFilter.Werte(Proben(), x => x.Hersteller));
        }

        [Fact]
        public void Die_Vorbelegung_der_Maximalfelder_kommt_aus_den_Daten()
        {
            var z = Proben();
            Assert.Equal(70, WaermepumpenKatalogFilter.GroessterVorlauf(z));
            Assert.Equal(99, WaermepumpenKatalogFilter.GroessteLeistung(z));

            // Ohne Zeilen bleibt es bei 0 statt einer Ausnahme aus Max().
            Assert.Equal(0, WaermepumpenKatalogFilter.GroessterVorlauf(Array.Empty<WaermepumpenKatalogZeile>()));
            Assert.Equal(0, WaermepumpenKatalogFilter.GroessteLeistung(null));
        }
    }

    /// <summary>
    /// <see cref="WPStammCtrl.KatalogZeilen"/> gegen die Testdatenbank (iU9-W7.0b) —
    /// die Anreicherung um kleinsten und groessten Vorlauf.
    /// </summary>
    [Collection("Testdatenbank")]
    public class KatalogZeilenTests
    {
        [Fact]
        public void Der_Katalog_traegt_jeden_Stammsatz_genau_einmal()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var ctrl = new WPStammCtrl();
            var zeilen = ctrl.KatalogZeilen();

            ctrl.ReadAll();
            Assert.Equal(ctrl.rows, zeilen.Count);
            Assert.Equal(ctrl.items.Select(m => m.WPName), zeilen.Select(z => z.Bezeichnung));
        }

        [Fact]
        public void Min_und_Max_Vorlauf_stammen_aus_den_Kennlinien()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var ctrl = new WPStammCtrl();
            var zeilen = ctrl.KatalogZeilen();
            ctrl.ReadAll();

            // Ein Geraet MIT Kennlinien suchen und beide Grenzen einzeln nachrechnen.
            bool geprueft = false;
            for (int i = 0; i < ctrl.rows && !geprueft; i++)
            {
                object min = DataRepository.ExecuteScalar(
                    "SELECT Min(Vorlauf) FROM " + WPStammCtrl.CURVE + " WHERE ID_WP = ?",
                    new DbParam("@id", ctrl.items[i].ID));
                if (min == null || min == DBNull.Value) continue;

                object max = DataRepository.ExecuteScalar(
                    "SELECT Max(Vorlauf) FROM " + WPStammCtrl.CURVE + " WHERE ID_WP = ?",
                    new DbParam("@id", ctrl.items[i].ID));

                Assert.Equal(Convert.ToDouble(min), zeilen[i].MinVorlauf);
                Assert.Equal(Convert.ToDouble(max), zeilen[i].MaxVorlauf);
                geprueft = true;
            }
            Assert.True(geprueft, "Kein Stammsatz mit Kennlinien in der Testdatenbank.");
        }

        [Fact]
        public void Die_Auslegung_folgt_der_Kuehlleistung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var ctrl = new WPStammCtrl();
            var zeilen = ctrl.KatalogZeilen();
            ctrl.ReadAll();

            for (int i = 0; i < ctrl.rows; i++)
            {
                string erwartet = ctrl.items[i].Kuehlleistung > 0
                    ? WaermepumpenKatalogZeile.AUSLEGUNG_HEIZEN_KUEHLEN
                    : WaermepumpenKatalogZeile.AUSLEGUNG_HEIZEN;
                Assert.Equal(erwartet, zeilen[i].Auslegung);
            }
        }
    }

    /// <summary>
    /// Das TOLERANTE Lesen der Betriebsart (<c>DbWerte.BetriebsartOderDefault</c>,
    /// Befund <b>W7‑B‑2</b> der Windows-Abnahme vom 06.09.2026).
    ///
    /// <para>Der Vorläufer <c>Wizard_WPItem</c> zeigte den gespeicherten Text in einer
    /// frei beschreibbaren <c>ComboBox</c> und schrieb ihn ungeprüft zurück; die
    /// Razor-Fassung hat ein <c>select</c> und zeigt einen nicht zeichengleichen Wert
    /// GAR NICHT — die Klappliste stand dann leer, und der OK-Knopf meldete „Bitte
    /// Betriebsart auswählen!". Diese Fälle halten fest, was die Lesekante daraus
    /// macht — ohne Datenbank und ohne Oberfläche.</para>
    /// </summary>
    public class WaermepumpenBetriebsartTests
    {
        [Theory]
        [InlineData("Alternativbetrieb", DbWerte.WP_BETRIEBSART_ALTERNATIV)]
        [InlineData("Parallelbetrieb", DbWerte.WP_BETRIEBSART_PARALLEL)]
        [InlineData("Teilparallelbetrieb", DbWerte.WP_BETRIEBSART_TEILPARALLEL)]
        public void Der_Steuerwert_bleibt_er_selbst(string gelesen, string erwartet)
        {
            Assert.Equal(erwartet, DbWerte.BetriebsartOderDefault(gelesen));
        }

        /// <summary>
        /// Gross-/Kleinschreibung, Leerzeichen und Bindestriche — die Schreibweisen,
        /// die eine frei beschreibbare ComboBox ueber die Jahre in die Spalte
        /// getragen haben kann (Befund L0-1).
        /// </summary>
        [Theory]
        [InlineData("parallelbetrieb", DbWerte.WP_BETRIEBSART_PARALLEL)]
        [InlineData("  Parallelbetrieb  ", DbWerte.WP_BETRIEBSART_PARALLEL)]
        [InlineData("Bivalent-parallel", DbWerte.WP_BETRIEBSART_PARALLEL)]
        [InlineData("bivalent parallel", DbWerte.WP_BETRIEBSART_PARALLEL)]
        [InlineData("Parallel operation", DbWerte.WP_BETRIEBSART_PARALLEL)]
        [InlineData("ALTERNATIVBETRIEB", DbWerte.WP_BETRIEBSART_ALTERNATIV)]
        [InlineData("bivalent-alternativ", DbWerte.WP_BETRIEBSART_ALTERNATIV)]
        [InlineData("Alternative operation", DbWerte.WP_BETRIEBSART_ALTERNATIV)]
        public void Ein_alter_Text_wird_auf_den_Steuerwert_gelesen(string gelesen, string erwartet)
        {
            Assert.Equal(erwartet, DbWerte.BetriebsartOderDefault(gelesen));
        }

        /// <summary>
        /// „teilparallel" enthaelt „parallel" — die Reihenfolge der Pruefung ist
        /// deshalb keine Geschmacksfrage.
        /// </summary>
        [Theory]
        [InlineData("Teilparallelbetrieb")]
        [InlineData("teilparallelbetrieb")]
        [InlineData("bivalent-teilparallel")]
        [InlineData("Partially parallel operation")]
        public void Teilparallel_wird_vor_Parallel_erkannt(string gelesen)
        {
            Assert.Equal(DbWerte.WP_BETRIEBSART_TEILPARALLEL,
                         DbWerte.BetriebsartOderDefault(gelesen));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("Monovalent")]
        [InlineData("42")]
        public void Unbekanntes_ergibt_leer(string gelesen)
        {
            Assert.Equal("", DbWerte.BetriebsartOderDefault(gelesen));
        }

        [Fact]
        public void DBNull_ergibt_leer()
        {
            Assert.Equal("", DbWerte.BetriebsartOderDefault(DBNull.Value));
        }
    }
}
