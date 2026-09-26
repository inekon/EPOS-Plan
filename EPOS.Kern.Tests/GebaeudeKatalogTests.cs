using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <see cref="GebaeudeStammCtrl"/> nach iU9-W9.0b — die Listen, die beiden Ableitungen,
    /// die bis dahin in <c>Form_Gebaeude</c> und <c>Form_Gebaeude1</c> standen, und der
    /// Filter der Katalogliste über die Zeilen des Katalogs; dazu <see cref="Suchmuster"/>
    /// aus W9.0e.
    ///
    /// <para>Die Faelle mit Datenbank laufen gegen eine ARBEITSKOPIE der Testdatenbank und
    /// schweigen, wenn es sie nicht gibt (<see cref="TestDatenbank"/>).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class GebaeudeKatalogTests
    {
        // ======================================================== Baualtersklassen

        /// <summary>Entscheid E47: 13 Klassen A bis M, Bauzeiträume nach IWU (2015) und Stein/Loga (2025).</summary>
        [Fact]
        public void Baualtersklassen_fuehrt_13_Bauzeitraeume()
        {
            using var kultur = new Kulturvorrichtung();
            Assert.Equal(13, GebaeudeStammCtrl.Baualtersklassen().Count);
            Assert.Equal(13, GebaeudeStammCtrl.BAUALTERSKLASSEN_DE.Length);
            Assert.Equal(new[]
            {
                "bis 1859", "1860 bis 1918", "1919 bis 1948", "1949 bis 1957", "1958 bis 1968", "1969 bis 1978",
                "1979 bis 1983", "1984 bis 1994", "1995 bis 2001", "2002 bis 2009", "2010 bis 2015", "2016 bis 2020",
                "ab 2021"
            }, GebaeudeStammCtrl.Baualtersklassen());
            Assert.Equal(GebaeudeStammCtrl.BAUALTERSKLASSEN_DE, GebaeudeStammCtrl.Baualtersklassen());
            Assert.Contains("IWU (2015)", Gebaeudeklassen.Quelle(), StringComparison.Ordinal);
            Assert.Contains("Stein/Loga (2025)", Gebaeudeklassen.Quelle(), StringComparison.Ordinal);
        }

        /// <summary>Die englischen Texte stehen in der Ressource (Glossar: building age class).</summary>
        [Fact]
        public void Baualtersklassen_sind_auf_Englisch_uebersetzt()
        {
            using var kultur = new Kulturvorrichtung("en-US");
            IReadOnlyList<string> en = GebaeudeStammCtrl.Baualtersklassen();
            Assert.Equal("up to 1859", en[0]);
            Assert.Equal("1958 to 1968", en[4]);
            Assert.Equal("from 2021", en[12]);
        }

        [Theory]
        [InlineData(0, 'A')]
        [InlineData(1, 'B')]
        [InlineData(12, 'M')]
        [InlineData(13, 'A')]     // ausserhalb der Liste -> A, wie im Vorlaeufer
        public void KlassenBuchstabe_bildet_den_Index_auf_A_bis_M_ab(int index, char erwartet)
        {
            Assert.Equal(erwartet, GebaeudeStammCtrl.KlassenBuchstabe(index));
        }

        [Theory]
        [InlineData("A", 0)]
        [InlineData("M", 12)]
        [InlineData("N", 0)]     // ein Buchstabe der alten Liste (A bis U) -> 0
        [InlineData("U", 0)]
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
            for (int i = 0; i < 13; i++)
                Assert.Equal(i, GebaeudeStammCtrl.KlassenIndex(
                    GebaeudeStammCtrl.KlassenBuchstabe(i).ToString()));
        }

        /// <summary>Der Klartext einer gespeicherten Klasse: leer und unbekannt bleiben leer (Bericht, Wohnflächenangabe).</summary>
        [Fact]
        public void Der_Klartext_faellt_nicht_auf_die_erste_Klasse_zurueck()
        {
            using var kultur = new Kulturvorrichtung();
            Assert.Equal("1958 bis 1968", Gebaeudeklassen.Text("E"));
            Assert.Equal("ab 2021", Gebaeudeklassen.Text("m"));
            Assert.Equal("", Gebaeudeklassen.Text("N"));
            Assert.Equal("", Gebaeudeklassen.Text(""));
            Assert.Equal("", Gebaeudeklassen.Text(null));
            Assert.Equal(GebaeudeStammCtrl.Klassentext("E"), Gebaeudeklassen.Text("E"));
        }

        /// <summary>DAS BAUJAHR FÜHRT (E47, F2): die Klasse aus dem Jahr, sonst die gewählte.</summary>
        [Theory]
        [InlineData(1965, 0, 4)]
        [InlineData(null, 7, 7)]
        [InlineData(1499, 7, 7)]      // ausserhalb des Bereichs: die Wahl bleibt
        [InlineData(2030, 2, 12)]
        public void Die_wirksame_Klasse_folgt_dem_Baujahr(int? baujahr, int gewaehlt, int erwartet)
        {
            Assert.Equal(erwartet, Gebaeudeklassen.IndexWirksam(baujahr, gewaehlt));
            Assert.Equal(baujahr is int j && j >= 1500 ? erwartet : (int?)null, Gebaeudeklassen.IndexAusBaujahr(baujahr));
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

        // ============================ Der Filter der Katalogliste (Stufe G3, Welle K)

        /// <summary>
        /// <b>Der Trichter „Verwendung" trifft genau die Wohngebäude</b> — der Filter, der bis
        /// Welle K als eigene SQL-Weiche im Projektdialog stand, läuft jetzt für Verwaltung und
        /// Projektdialog über die Katalogliste im Kern. Der Anzeigetext „Wohngebäude" enthält
        /// „Gewerbe+Sonstige" nicht und umgekehrt; der Trichter trifft deshalb genau die Sätze
        /// mit dem Steuerwert.
        /// </summary>
        [Fact]
        public void Der_Trichter_Verwendung_trifft_genau_die_Saetze_der_Verwendung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            IReadOnlyList<Katalogfilterzeile> zeilen = GebaeudeStammCtrl.Katalogfilterzeilen();

            // Die Gegenrechnung ist die SQL-Bedingung auf dem Steuerwert: Ein Satz OHNE
            // Verwendung (NULL) steht in keiner der beiden Mengen - in der Liste traegt er eine
            // leere Zelle, die kein Trichter trifft.
            foreach ((string steuerwert, string bedingung) in new[]
            {
                (GebaeudeStammCtrl.FILTERWERT_WOHN, GebaeudeStammCtrl.FILTER_WOHNGEBAEUDE),
                (GebaeudeStammCtrl.FILTERWERT_SONSTIGE, GebaeudeStammCtrl.FILTER_NICHT_WOHNGEBAEUDE)
            })
            {
                var stand = new Katalogfilterstand();
                stand.Setzen(Katalogfilterprofil.SpVerwendung, GebaeudeStammCtrl.Verwendungstext(steuerwert));

                IReadOnlyList<Katalogfilterzeile> treffer =
                    Katalogfilter.Anwenden(Katalogfilterprofil.FuerGebaeude(), zeilen, stand);

                var ctrl = new GebaeudeStammCtrl();
                ctrl.ReadAll(bedingung);
                var erwartet = ctrl.items.Select(m => m.Gebaeudename).OrderBy(n => n, StringComparer.Ordinal).ToList();

                Assert.NotEmpty(treffer);
                Assert.Equal(erwartet, treffer.Select(z => z.Bezeichner).OrderBy(n => n, StringComparer.Ordinal));
            }
        }

        /// <summary>
        /// Der Trichter „Baujahr" filtert auf dem KLARTEXT der Baualtersklasse — derselbe Text,
        /// der in der Zelle steht, nicht ihr Buchstabe.
        /// </summary>
        [Fact]
        public void Der_Trichter_Baujahr_trifft_die_Klasse_ueber_ihren_Klartext()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            IReadOnlyList<Katalogfilterzeile> zeilen = GebaeudeStammCtrl.Katalogfilterzeilen();
            var ctrl = new GebaeudeStammCtrl();
            ctrl.ReadAll();

            GebaeudeModel erster = ctrl.items.FirstOrDefault(m => !string.IsNullOrEmpty(m.Baualtersklasse));
            if (erster is null) return;
            string klartext = GebaeudeStammCtrl.Baualtersklassen()[GebaeudeStammCtrl.KlassenIndex(erster.Baualtersklasse)];

            var stand = new Katalogfilterstand();
            stand.Setzen(Katalogfilterprofil.SpBaualtersklasse, klartext);
            IReadOnlyList<Katalogfilterzeile> treffer =
                Katalogfilter.Anwenden(Katalogfilterprofil.FuerGebaeude(), zeilen, stand);

            Assert.Contains(treffer, z => z.Bezeichner == erster.Gebaeudename);
            Assert.All(treffer, z => Assert.Contains(klartext, z.Text(Katalogfilterprofil.SpBaualtersklasse)));
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
