using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <see cref="GebaeudeStammCtrl"/> nach iU9-W9.0b — die Listen, der Katalogfilter und
    /// die beiden Ableitungen, die bis dahin in <c>Form_Gebaeude</c> und
    /// <c>Form_Gebaeude1</c> standen; dazu <see cref="Suchmuster"/> aus W9.0e.
    ///
    /// <para>Die Faelle mit Datenbank laufen gegen eine ARBEITSKOPIE der Testdatenbank und
    /// schweigen, wenn es sie nicht gibt (<see cref="TestDatenbank"/>).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class GebaeudeKatalogTests
    {
        // ======================================================== Baualtersklassen

        [Fact]
        public void Baualtersklassen_fuehrt_21_Eintraege()
        {
            Assert.Equal(21, GebaeudeStammCtrl.Baualtersklassen().Count);
            Assert.Equal(21, GebaeudeStammCtrl.BAUALTERSKLASSEN_DE.Length);
        }

        [Theory]
        [InlineData(0, 'A')]
        [InlineData(1, 'B')]
        [InlineData(20, 'U')]
        public void KlassenBuchstabe_bildet_den_Index_auf_A_bis_U_ab(int index, char erwartet)
        {
            Assert.Equal(erwartet, GebaeudeStammCtrl.KlassenBuchstabe(index));
        }

        [Theory]
        [InlineData("A", 0)]
        [InlineData("U", 20)]
        [InlineData("", 0)]
        [InlineData("1", 0)]     // negativ -> 0, wie im Vorlaeufer
        [InlineData("z", 0)]     // ausserhalb der Liste -> 0
        public void KlassenIndex_ist_der_Rueckweg(string klasse, int erwartet)
        {
            Assert.Equal(erwartet, GebaeudeStammCtrl.KlassenIndex(klasse));
        }

        [Fact]
        public void KlassenBuchstabe_und_KlassenIndex_sind_umkehrbar()
        {
            for (int i = 0; i < 21; i++)
                Assert.Equal(i, GebaeudeStammCtrl.KlassenIndex(
                    GebaeudeStammCtrl.KlassenBuchstabe(i).ToString()));
        }

        // ============================================================ Bauart

        [Theory]
        [InlineData(2000, 100, 0)]   // spez = 20  -> leicht
        [InlineData(5000, 100, 1)]   // spez = 50  -> schwer
        [InlineData(10000, 100, 2)]  // spez = 100 -> sehr schwer
        [InlineData(3000, 100, 1)]   // spez = 30  -> genau die Grenze, also schwer
        [InlineData(7500, 100, 1)]   // spez = 75  -> genau die Grenze, also schwer
        [InlineData(0, 0, 1)]        // Wohnflaeche 0 -> schwer (NaN im Vorlaeufer)
        public void BauartAusBauweise_bildet_die_drei_Stufen(double bauweise, double wfl, int erwartet)
        {
            Assert.Equal(erwartet, GebaeudeStammCtrl.BauartAusBauweise(bauweise, wfl));
        }

        [Theory]
        [InlineData(0, 100, 2000.0)]
        [InlineData(1, 100, 5000.0)]
        [InlineData(2, 100, 10000.0)]
        [InlineData(7, 100, 50.0)]    // jeder andere Index -> 50 (Befund W9-B6)
        [InlineData(-1, 100, 50.0)]
        public void BauweiseAusBauart_ist_der_Rueckweg(int index, double wfl, double erwartet)
        {
            Assert.Equal(erwartet, GebaeudeStammCtrl.BauweiseAusBauart(index, wfl));
        }

        // ====================================================== Filterausdruck

        [Fact]
        public void FilterAusdruck_ohne_Auswahl_ist_nur_die_Verwendung()
        {
            Assert.Equal(GebaeudeStammCtrl.FILTER_WOHNGEBAEUDE,
                GebaeudeStammCtrl.FilterAusdruck(true, null, null, false));
            Assert.Equal(GebaeudeStammCtrl.FILTER_NICHT_WOHNGEBAEUDE,
                GebaeudeStammCtrl.FilterAusdruck(false, null, null, false));
        }

        [Fact]
        public void FilterAusdruck_nur_Baujahr_haengt_die_Verwendung_an()
        {
            Assert.Equal("Baualtersklasse='C' and " + GebaeudeStammCtrl.FILTER_WOHNGEBAEUDE,
                GebaeudeStammCtrl.FilterAusdruck(true, null, 2, true));
        }

        /// <summary>
        /// <b>Befund W9-B1.</b> Derselbe Zustand — Gebaeudeart gewaehlt, Baujahr „Alle" —
        /// ergibt in den beiden Handlern des Vorlaeufers ZWEI verschiedene Ausdruecke.
        /// Woertlich uebernommen; der Fall haelt beide fest.
        /// </summary>
        [Fact]
        public void FilterAusdruck_Befund_B1_Gebaeudeart_ohne_Baujahr_haengt_am_ausloesenden_Feld()
        {
            Assert.Equal("Gebaeudeart='Hotel'",
                GebaeudeStammCtrl.FilterAusdruck(true, "Hotel", null, false));   // :359

            Assert.Equal("Gebaeudeart='Hotel' and " + GebaeudeStammCtrl.FILTER_WOHNGEBAEUDE,
                GebaeudeStammCtrl.FilterAusdruck(true, "Hotel", null, true));    // :392
        }

        [Fact]
        public void FilterAusdruck_mit_beidem_nennt_alle_drei_Bedingungen()
        {
            const string erwartet = "Gebaeudeart='Hotel' and Baualtersklasse='A' and " +
                                    GebaeudeStammCtrl.FILTER_WOHNGEBAEUDE;
            Assert.Equal(erwartet, GebaeudeStammCtrl.FilterAusdruck(true, "Hotel", 0, false));
            Assert.Equal(erwartet, GebaeudeStammCtrl.FilterAusdruck(true, "Hotel", 0, true));
        }

        // ================================================= Listen aus der Datenbank

        [Fact]
        public void Gebaeudearten_trennt_Wohn_und_Nichtwohngebaeude()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            IReadOnlyList<string> wohn = GebaeudeStammCtrl.Gebaeudearten(true);
            IReadOnlyList<string> sonstige = GebaeudeStammCtrl.Gebaeudearten(false);
            IReadOnlyList<string> alle = GebaeudeStammCtrl.Gebaeudearten(null);

            Assert.NotEmpty(wohn);
            Assert.NotEmpty(sonstige);
            Assert.Empty(wohn.Intersect(sonstige));
            Assert.Equal(alle.Count, wohn.Count + sonstige.Count);
        }

        [Fact]
        public void Gebaeudetypen_liefert_die_Sicht_Abfrage_Gebaeudetypen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.NotEmpty(GebaeudeStammCtrl.Gebaeudetypen());
        }

        [Fact]
        public void Katalognamen_liefert_alle_Stammsaetze()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var ctrl = new GebaeudeStammCtrl();
            ctrl.ReadAll();

            Assert.Equal(ctrl.rows, GebaeudeStammCtrl.Katalognamen().Count);
        }

        [Fact]
        public void Filtern_liefert_nur_Saetze_der_gewaehlten_Verwendung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var ctrl = new GebaeudeStammCtrl();
            IReadOnlyList<GebaeudeModel> wohn = ctrl.Filtern(true, null, null, false);

            Assert.All(wohn, m => Assert.Equal("Wohngebaeude", m.Wohngebaeude_Nicht_Wohngebaeude));
        }

        [Fact]
        public void Filtern_mit_Baujahr_liefert_nur_die_gewaehlte_Klasse()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var ctrl = new GebaeudeStammCtrl();
            IReadOnlyList<GebaeudeModel> treffer = ctrl.Filtern(true, null, 0, true);

            Assert.All(treffer, m => Assert.Equal("A", m.Baualtersklasse));
        }

        // ============================================================ Suchmuster

        [Fact]
        public void Suchmuster_ohne_Platzhalter_sucht_als_Teilstring()
        {
            var muster = Suchmuster.Uebersetzen("haus");

            Assert.True(Suchmuster.Trifft(muster, "Einfamilienhaus"));
            Assert.True(Suchmuster.Trifft(muster, "HAUS 1"));
            Assert.False(Suchmuster.Trifft(muster, "Hotel"));
        }

        [Fact]
        public void Suchmuster_mit_Stern_verankert()
        {
            var muster = Suchmuster.Uebersetzen("Haus*");

            Assert.True(Suchmuster.Trifft(muster, "Haus 1990"));
            Assert.False(Suchmuster.Trifft(muster, "Reihenhaus"));
        }

        [Fact]
        public void Suchmuster_mit_Fragezeichen_trifft_genau_ein_Zeichen()
        {
            var muster = Suchmuster.Uebersetzen("Haus?");

            Assert.True(Suchmuster.Trifft(muster, "Haus1"));
            Assert.False(Suchmuster.Trifft(muster, "Haus12"));
        }

        [Fact]
        public void Suchmuster_ohne_Eingabe_und_bei_Stern_ist_kein_Filter()
        {
            Assert.Null(Suchmuster.Uebersetzen(""));
            Assert.Null(Suchmuster.Uebersetzen("   "));
            Assert.Null(Suchmuster.Uebersetzen(null));
            Assert.Null(Suchmuster.Uebersetzen("*"));
            Assert.True(Suchmuster.Trifft(null, "irgendetwas"));
        }

        [Fact]
        public void Suchmuster_findet_ueber_einen_Zeilenumbruch_hinweg()
        {
            // Die Katalogzelle traegt "Art\nFlaeche [m²]" in EINER Zelle.
            var muster = Suchmuster.Uebersetzen("Hotel 120");

            Assert.True(Suchmuster.Trifft(muster, "Hotel\n120 [m²]"));
        }
    }
}
