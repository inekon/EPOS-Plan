using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Namensabgleich N1…N7 ohne Datenbank</b> (Mehrzonenkonzept 3.5 und 6.3): jede Stufe mit den
    /// Beispielen der Tabelle 3.5 und den gemessenen Namen des Befunds P, § 3.6; der Vorrang der
    /// Anwenderzuordnung; Herstellerzeile gegen herstellerneutrale Zeile; Luftschicht und Schraffur; das
    /// Plausibilitätsband; die Synonymsaat (normalisiert, eindeutig, neutral, mit Quelle).
    /// </summary>
    public class BaustoffabgleichTests
    {
        private static readonly Baustoffabgleich Saat = new Baustoffabgleich(BaustoffabgleichDaten.AusSaat());

        private static Abgleichtreffer A(string name) => Saat.Abgleichen(name);

        private static void Trifft(string name, Abgleichstufe stufe, int id)
        {
            Abgleichtreffer t = A(name);
            Assert.True(t.Stufe == stufe && t.Baustoff?.ID == id,
                        name + ": erwartet " + stufe + " → " + id + ", ist " + t + " (" + t.Beleg + ")");
        }

        // =====================================================================
        //  N1 und N2
        // =====================================================================

        [Theory]
        [InlineData("Leichtbeton 102890359", "Leichtbeton")]
        [InlineData("Stahlbeton 65690", "Stahlbeton")]
        [InlineData("Kalksandstein 2816491304", "Kalksandstein")]
        [InlineData("Radial Gradient Fill 1515460218", "Radial Gradient Fill")]
        [InlineData("Solid 397409098", "Solid")]
        [InlineData("Kalksandstein 1800", "Kalksandstein 1800")]      // vier Ziffern: eine Rohdichte, keine Kennung
        [InlineData("  Holz  ", "Holz")]
        [InlineData("65690", "65690")]                                 // nur Ziffern: bleibt stehen
        [InlineData(null, "")]
        public void N1_schneidet_die_Kennung_ab(string roh, string erwartet)
            => Assert.Equal(erwartet, Baustoffabgleich.OhneZahlenschwanz(roh));

        [Fact]
        public void N2_normalisiert_und_trennt_Marken_ab()
        {
            Baustoffname n = Baustoffabgleich.Normalisieren("Ortbeton - bewehrt Verputzt");
            Assert.Equal("ortbeton", n.Kern);
            Assert.Equal("ortbeton bewehrt verputzt", n.Voll);
            Assert.Equal(new[] { "bewehrt", "verputzt" }, n.Marken);

            Assert.Equal("fussbodenaufbau", Baustoffabgleich.Normalisieren("Fußbodenaufbau").Kern);
            Assert.Equal("mauerwerk naturstein", Baustoffabgleich.Normalisieren("Mauerwerk - Naturstein").Kern);
            Assert.Equal("concrete cast in place gray", Baustoffabgleich.Normalisieren("Concrete, Cast-in-Place gray").Kern);
            Assert.Equal("insulation thermal barriers rigid insulation",
                         Baustoffabgleich.Normalisieren("Insulation / Thermal Barriers - Rigid insulation").Kern);
            // Ein Komma zwischen zwei Ziffern bleibt; Akzente fallen weg.
            Assert.Equal("mineralwolle λd 0,035", Baustoffabgleich.Normalisieren("Mineralwolle λD 0,035").Kern);
            Assert.Equal("beton", Baustoffabgleich.Normalisieren("Béton").Kern);
            // Maßangaben sind Marken, getrennt geschrieben oder zusammen.
            Baustoffname m = Baustoffabgleich.Normalisieren("Mauerwerk 24 cm");
            Assert.Equal("mauerwerk", m.Kern);
            Assert.Equal(new[] { "24 cm" }, m.Marken);
            Assert.Equal("concrete", Baustoffabgleich.Normalisieren("Generic - 200mm Concrete").Kern);
            // Nur Marken: dann ist der Kern der volle Name.
            Assert.Equal("generisch", Baustoffabgleich.Normalisieren("Generisch").Kern);
            // Der Schlüssel einer Zuordnung ist der Kern, gekürzt auf die Spaltenlänge.
            Assert.Equal("kalksandstein", Baustoffabgleich.Schluessel("Kalksandstein - verputzt 2816491304"));
            Assert.Equal(BaustoffabgleichSchema.LAENGE_MATERIALNAME, Baustoffabgleich.Schluessel(new string('x', 300)).Length);
        }

        // =====================================================================
        //  N3 bis N7 — die Beispiele der Tabelle 3.5 und die gemessenen Namen
        // =====================================================================

        [Fact]
        public void N3_trifft_den_Bezeichner_genau()
        {
            Trifft("Stahlbeton 65690", Abgleichstufe.Genau, 10);                  // `stahlbeton` → Stahlbeton
            Trifft("Aluminium 131198", Abgleichstufe.Genau, 61);
            Trifft("Kalksandstein 1800", Abgleichstufe.Genau, 20);
            Trifft("Calciumsulfat-Fließestrich", Abgleichstufe.Genau, 7);
            Trifft("Mineralwolle λD 0,040", Abgleichstufe.Genau, 37);
            Assert.Equal(Baustoffabgleich.BELEG_N3, A("Stahlbeton").Beleg.Schluessel);
        }

        [Fact]
        public void N4_trifft_ueber_die_Synonyme_zweisprachig()
        {
            Trifft("reinforced concrete", Abgleichstufe.Synonym, 10);             // Tabelle 3.5
            Trifft("Leichtbeton 102890359", Abgleichstufe.Synonym, 12);
            Trifft("Kalksandstein 2816491304", Abgleichstufe.Synonym, 20);
            Trifft("Ortbeton - bewehrt", Abgleichstufe.Synonym, 10);
            Trifft("Ortbeton - bewehrt Verputzt", Abgleichstufe.Synonym, 10);
            Trifft("Mauerwerk - Naturstein", Abgleichstufe.Synonym, 27);
            Trifft("Holz", Abgleichstufe.Synonym, 29);
            Trifft("Dämmung", Abgleichstufe.Synonym, 36);
            Trifft("insulation", Abgleichstufe.Synonym, 36);
            Trifft("brick", Abgleichstufe.Synonym, 13);
            Trifft("Gypsum Wall Board", Abgleichstufe.Synonym, 48);
            Trifft("Beton 20 cm", Abgleichstufe.Synonym, 9);                       // Maßangabe als Marke
            Abgleichtreffer t = A("reinforced concrete");
            Assert.Equal(Baustoffabgleich.BELEG_N4, t.Beleg.Schluessel);
            Assert.Equal(new[] { "reinforced concrete", "en", "Stahlbeton" }, t.Beleg.Werte);
        }

        [Fact]
        public void N4_trifft_ein_Synonym_als_Wortanfang_und_waehlt_die_Rohdichtestufe()
        {
            Trifft("Concrete, Cast-in-Place gray", Abgleichstufe.Synonym, 10);    // Revit: Farbe angehängt
            Assert.Equal(Baustoffabgleich.BELEG_N4_WORTANFANG, A("Concrete, Cast-in-Place gray").Beleg.Schluessel);
            Trifft("KS 1400", Abgleichstufe.Synonym, 18);                          // die Zahl wählt die Stufe der Reihe
            Trifft("Porenbeton PP4 600", Abgleichstufe.Synonym, 25);
            Trifft("KS 1500", Abgleichstufe.Synonym, 20);                          // keine Stufe 1500: der Vertreter
            Trifft("EPS 035", Abgleichstufe.Synonym, 39);
        }

        [Fact]
        public void N5_trifft_ein_eindeutiges_Teilwort_und_laesst_Mehrdeutiges_offen()
        {
            Trifft("Stahlbetondecke", Abgleichstufe.Teilwort, 10);                // Grundeintrag vor „Stahlbeton 1 % Bewehrung"
            Trifft("Kalksandsteinwand", Abgleichstufe.Teilwort, 20);              // Grundeintrag über das Synonym
            Trifft("Mineralwolledämmung", Abgleichstufe.Teilwort, 36);
            Trifft("Stahlträger", Abgleichstufe.Teilwort, 60);
            Trifft("Gipsputz", Abgleichstufe.Teilwort, 2);                         // das Katalogwort als ganzes Wort
            Assert.Equal(Baustoffabgleich.BELEG_N5, A("Stahlträger").Beleg.Schluessel);

            // Tabelle 3.5: kein eindeutiger Treffer.
            Abgleichtreffer fb = A("Fußbodenaufbau");
            Assert.Equal(Abgleichstufe.Keine, fb.Stufe);
            Assert.False(fb.Getroffen);
            Assert.Equal(Baustoffabgleich.BELEG_OHNE, fb.Beleg.Schluessel);

            // Zwei Wörter, zwei Stoffe: mehrdeutig.
            Abgleichtreffer zwei = A("Wand Kalksandstein Porenbeton");
            Assert.Equal(Abgleichstufe.Keine, zwei.Stufe);
            Assert.Equal(2, zwei.Kandidaten);
            Assert.Equal(Baustoffabgleich.BELEG_MEHRDEUTIG, zwei.Beleg.Schluessel);

            // Zu kurze Wortanfänge öffnen nichts.
            Assert.False(A("Holzwerkstoffplatte").Getroffen);
            Assert.False(A("Putzträgerplatte").Getroffen);
        }

        [Fact]
        public void N6_Luftschicht_und_Schraffur()
        {
            foreach (string luft in new[] { "Luftschicht", "Air", "air gap", "Luftschicht 4 cm", "Luftschicht, ruhend" })
            {
                Abgleichtreffer t = A(luft);
                Assert.True(t.Stufe == Abgleichstufe.Sonderfall && t.Sonderfall == Abgleichsonderfall.Luftschicht, luft + ": " + t);
                Assert.Null(t.Baustoff);
                Assert.Equal(Baustoffabgleich.BELEG_N6_LUFTSCHICHT, t.Beleg.Schluessel);
            }
            foreach (string weg in new[] { "Solid 397409098", "Radial Gradient Fill 1515460218", "Leer", "Empty Fill" })
            {
                Abgleichtreffer t = A(weg);
                Assert.True(t.Stufe == Abgleichstufe.Sonderfall && t.Sonderfall == Abgleichsonderfall.Verwerfen, weg + ": " + t);
                Assert.Equal(Baustoffabgleich.BELEG_N6_VERWORFEN, t.Beleg.Schluessel);
            }
        }

        [Fact]
        public void N7_die_gemerkte_Zuordnung_gilt_vor_allen_Stufen()
        {
            var abgleich = new Baustoffabgleich(BaustoffabgleichDaten.AusSaat(new[]
            {
                new BaustoffNamenzuordnung("fussbodenaufbau", 5),            // ohne Treffer der Kette
                new BaustoffNamenzuordnung("holz", 30),                      // N4 träfe Nadelholz
                new BaustoffNamenzuordnung("stahlbeton", 11),                // N3 träfe Stahlbeton
                new BaustoffNamenzuordnung("luftschicht", 55),               // N6 träfe die Luftschicht
                new BaustoffNamenzuordnung("unbekannt", 99999),              // Katalogbaustoff fehlt: übergangen
            }));
            Assert.Equal((Abgleichstufe.Anwender, 5), Stufe(abgleich.Abgleichen("Fußbodenaufbau")));
            Assert.Equal((Abgleichstufe.Anwender, 30), Stufe(abgleich.Abgleichen("Holz")));
            Assert.Equal((Abgleichstufe.Anwender, 11), Stufe(abgleich.Abgleichen("Stahlbeton 65690")));
            Assert.Equal((Abgleichstufe.Anwender, 55), Stufe(abgleich.Abgleichen("Luftschicht")));
            Assert.Equal(Abgleichstufe.Keine, abgleich.Abgleichen("unbekannt").Stufe);
            Abgleichtreffer t = abgleich.Abgleichen("Fußbodenaufbau");
            Assert.Equal(Baustoffabgleich.BELEG_N7, t.Beleg.Schluessel);
            Assert.Equal("N7", t.StufeKurz);
        }

        private static (Abgleichstufe, int) Stufe(Abgleichtreffer t) => (t.Stufe, t.Baustoff?.ID ?? 0);

        // =====================================================================
        //  Herstellerzeilen (E39)
        // =====================================================================

        /// <summary>
        /// Eine Herstellerzeile trifft nur mit ihrem genauen Namen und nur, wenn keine herstellerneutrale
        /// Zeile genau trifft; ein Teilwort (N5) trifft sie nie.
        /// </summary>
        [Fact]
        public void Herstellerzeilen_nur_beim_genauen_Namen_und_nach_der_neutralen_Zeile()
        {
            var daten = new BaustoffabgleichDaten(new[]
            {
                new BaustoffModel { ID = 24, Bezeichner = "Porenbeton 500", Lambda = 0.16, Rho = 500, Cp = 1000 },
                new BaustoffModel { ID = 1002, Bezeichner = "Porenbeton 500", Hersteller = "Hersteller A", Lambda = 0.12, Rho = 500, Cp = 1000 },
                new BaustoffModel { ID = 1003, Bezeichner = "Planstein PP6", Hersteller = "Hersteller A", Lambda = 0.18, Rho = 650, Cp = 1000 },
                new BaustoffModel { ID = 1004, Bezeichner = "Planstein PP6", Hersteller = "Hersteller B", Lambda = 0.19, Rho = 650, Cp = 1000 },
                new BaustoffModel { ID = 1005, Bezeichner = "Wärmedämmstein W1", Hersteller = "Hersteller B", Lambda = 0.08, Rho = 400, Cp = 1000 },
            }, null);
            var abgleich = new Baustoffabgleich(daten);

            Assert.Equal((Abgleichstufe.Genau, 24), Stufe(abgleich.Abgleichen("Porenbeton 500")));     // neutral vor Hersteller
            Abgleichtreffer w = abgleich.Abgleichen("Wärmedämmstein W1");
            Assert.Equal((Abgleichstufe.Genau, 1005), Stufe(w));                                         // nur beim genauen Namen
            Assert.Equal(Baustoffabgleich.BELEG_N3_HERSTELLER, w.Beleg.Schluessel);
            Assert.Equal(new[] { "Wärmedämmstein W1", "Hersteller B" }, w.Beleg.Werte);
            Assert.False(abgleich.Abgleichen("Planstein PP6").Getroffen);                               // zwei Hersteller: offen
            Assert.False(abgleich.Abgleichen("Wärmedämmsteinwand").Getroffen);                          // N5 nie auf Hersteller
            Assert.False(abgleich.Abgleichen("Planstein").Getroffen);
        }

        [Fact]
        public void Die_Saat_trifft_jede_Herstellerzeile_nur_mit_ihrem_Namen()
        {
            foreach (BaustoffSaat s in BaustoffSchema.Saat.Where(x => x.Hersteller != null))
            {
                Abgleichtreffer t = A(s.Bezeichner);
                // Entweder die Herstellerzeile genau — oder eine herstellerneutrale Zeile gleichen Namens geht vor.
                Assert.True(t.Stufe == Abgleichstufe.Genau, s.Bezeichner + ": " + t);
                if (t.Baustoff.ID != s.Id) Assert.Null(t.Baustoff.Hersteller);
            }
        }

        // =====================================================================
        //  Plausibilitätsband
        // =====================================================================

        [Fact]
        public void Ein_Stoffwert_kleiner_gleich_null_oder_ausserhalb_des_Bands_ist_kein_Wert()
        {
            Assert.False(Baustoffabgleich.LambdaImBand(0.0));
            Assert.False(Baustoffabgleich.LambdaImBand(null));
            Assert.False(Baustoffabgleich.LambdaImBand(0.0049));
            Assert.True(Baustoffabgleich.LambdaImBand(0.005));
            Assert.True(Baustoffabgleich.LambdaImBand(500.0));
            Assert.False(Baustoffabgleich.LambdaImBand(500.1));
            Assert.False(Baustoffabgleich.RhoImBand(0.0));
            Assert.False(Baustoffabgleich.RhoImBand(4.9));
            Assert.True(Baustoffabgleich.RhoImBand(5.0));
            Assert.True(Baustoffabgleich.RhoImBand(8000.0));
            Assert.False(Baustoffabgleich.RhoImBand(8000.5));
            Assert.False(Baustoffabgleich.CpImBand(99.0));
            Assert.True(Baustoffabgleich.CpImBand(100.0));
            Assert.True(Baustoffabgleich.CpImBand(5000.0));
            Assert.False(Baustoffabgleich.CpImBand(5001.0));
            Assert.True(Baustoffabgleich.AusserhalbDesBands(1000.0, Baustoffabgleich.LambdaImBand));
            Assert.False(Baustoffabgleich.AusserhalbDesBands(null, Baustoffabgleich.LambdaImBand));
            // Dasselbe Band wie die Verwaltung und die Reduktion.
            Assert.Equal(BaustoffCtrl.LAMBDA_MIN, GebaeudeFestwerte.LAMBDA_MIN_WMK);
            Assert.Equal(BaustoffCtrl.RHO_MAX, GebaeudeFestwerte.ROHDICHTE_MAX_KGM3);
            Assert.Equal(BaustoffCtrl.CP_MIN, GebaeudeFestwerte.CP_MIN_JKGK);
        }

        // =====================================================================
        //  Die Synonymsaat
        // =====================================================================

        [Fact]
        public void Die_Synonymsaat_ist_normalisiert_eindeutig_neutral_und_belegt()
        {
            IReadOnlyList<BaustoffsynonymSaat> saat = BaustoffabgleichSchema.Saat;
            Assert.Equal(212, saat.Count);
            Assert.Equal(Enumerable.Range(1, saat.Count), saat.Select(s => s.Id));
            Assert.True(saat.Count < BaustoffabgleichSchema.SAAT_ID_GRENZE);
            Assert.Equal(saat.Count, saat.Select(s => s.Materialname).Distinct(StringComparer.Ordinal).Count());

            var neutral = BaustoffSchema.Saat.Where(b => b.Hersteller == null).ToDictionary(b => b.Id);
            var kerne = new HashSet<string>(BaustoffSchema.Saat.Select(b => Baustoffabgleich.Normalisieren(b.Bezeichner).Kern), StringComparer.Ordinal);
            // Die Markenwörter der Herstellerzeilen — ohne die Gattungswörter, die einzelne Firmennamen
            // tragen („… Insulation", „KS-Original"); die Gattung selbst ist kein Herstellername.
            var gattung = new HashSet<string>(StringComparer.Ordinal) { "insulation", "original" };
            var herstellerwoerter = new HashSet<string>(BaustoffSchema.Saat.Where(b => b.Hersteller != null)
                .SelectMany(b => Baustoffabgleich.Normalisieren(b.Hersteller).Kern.Split(' '))
                .Where(w => w.Length >= 3 && !gattung.Contains(w)), StringComparer.Ordinal);
            Assert.Contains("rockwool", herstellerwoerter);
            foreach (BaustoffsynonymSaat s in saat)
            {
                Assert.True(s.Materialname == Baustoffabgleich.Normalisieren(s.Materialname).Kern, s.Materialname + ": nicht normalisiert");
                Assert.Contains(s.Sprache, BaustoffabgleichSchema.SPRACHEN);
                Assert.True(neutral.ContainsKey(s.IdBaustoff), s.Materialname + ": kein herstellerneutraler Baustoff " + s.IdBaustoff);
                Assert.False(string.IsNullOrWhiteSpace(s.Quelle), s.Materialname + ": ohne Quelle");
                Assert.True(s.Quelle.Length <= BaustoffabgleichSchema.LAENGE_QUELLE, s.Materialname + ": Quelle zu lang");
                Assert.False(kerne.Contains(s.Materialname), s.Materialname + ": gleicht einem Bezeichner (träfe schon N3)");
                foreach (string w in s.Materialname.Split(' '))
                    Assert.False(herstellerwoerter.Contains(w), s.Materialname + ": enthält den Herstellernamen „" + w + "“");
            }
            Assert.True(saat.Count(s => s.Sprache == "de") > 50 && saat.Count(s => s.Sprache == "en") > 50, "nicht zweisprachig");
            // Jeder der 65 herstellerneutralen Stoffe ist über den Abgleich erreichbar — durch einen Namen oder ein Synonym.
            Assert.Equal(65, neutral.Count);
        }

        [Fact]
        public void Die_Saat_der_Auslieferung_traegt_Katalog_und_Synonyme()
        {
            Assert.Equal(BaustoffSchema.Saat.Count, Saat.Katalogzahl);
            Assert.Equal(BaustoffabgleichSchema.Saat.Count, Saat.Synonymzahl);
            // Ein Synonym auf einen fehlenden Baustoff wird übergangen, ebenso ein zweites mit demselben Namen.
            var abgleich = new Baustoffabgleich(new BaustoffabgleichDaten(
                new[] { new BaustoffModel { ID = 1, Bezeichner = "Kalkzementputz", Lambda = 1.0, Rho = 1800, Cp = 1000 } },
                new[] { new BaustoffSynonym("putz", "de", 1, "Probe"), new BaustoffSynonym("Putz", "de", 2, "Probe"),
                        new BaustoffSynonym("render", "en", 77, "Probe") }));
            Assert.Equal(1, abgleich.Synonymzahl);
            Assert.Equal((Abgleichstufe.Synonym, 1), Stufe(abgleich.Abgleichen("Putz")));
            Assert.False(abgleich.Abgleichen("render").Getroffen);
        }
    }
}
