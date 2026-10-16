using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Das Zeichenmodell des Energieflussbilds</b> (Konzept Energieflussbild 3 und 6, Schritt SK-2): Layout nach Spalten,
    /// Bänder, die an ihre Knoten anschließen, Mindestbandbreite, breite, schmale und Berichtsfassung, Zeigertexte an jedem
    /// Band und Knoten, keine Zahl im Bildschirmbild (SK-Q12), Rollen statt Farbwerten, Determinismus.
    ///
    /// <para>Die Gaben sind synthetisch: zwei Quellen, ein Wandler mit Verlust, der Strombus, ein Speicher mit Verlust und
    /// drei Senken, eine davon „nicht zugeordnet“. Die Klasse steht in der Sammlung „Testdatenbank“, weil das PNG gegen
    /// <c>Farbpalette.Aktuell</c> malt — prozessweiter Zustand, den die Farbtests setzen.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class EnergieflussModellTests
    {
        private const float KB = ChartRenderer.EF_KNOTENBREITE;

        private static EnergieflussBildDaten Gaben(double klein = 6.0)
        {
            var knoten = new List<EnergieflussBildKnoten>
            {
                new EnergieflussBildKnoten { Kennung = "netz", Spalte = 0, Name = "Netzbezug", Kurzname = "Netz", Art = EnergieflussKnotenart.Quelle },
                new EnergieflussBildKnoten { Kennung = "gas", Spalte = 0, Name = "Erdgas", Art = EnergieflussKnotenart.Quelle },
                new EnergieflussBildKnoten { Kennung = "bhkw", Spalte = 1, Name = "BHKW", Art = EnergieflussKnotenart.Wandler },
                new EnergieflussBildKnoten { Kennung = "v_bhkw", Spalte = 1, Name = "Verlust", Art = EnergieflussKnotenart.Verlust },
                new EnergieflussBildKnoten { Kennung = "strom", Spalte = 2, Name = "Strom", Art = EnergieflussKnotenart.Bus,
                                             Zeigertext = "Strom: Zufluss 60,0 · Abfluss 60,0 MWh/a" },
                new EnergieflussBildKnoten { Kennung = "batterie", Spalte = 5, Name = "Stromspeicher", Art = EnergieflussKnotenart.Speicher },
                new EnergieflussBildKnoten { Kennung = "v_batterie", Spalte = 5, Name = "Verlust", Art = EnergieflussKnotenart.Verlust },
                new EnergieflussBildKnoten { Kennung = "bedarf", Spalte = 6, Name = "Strombedarf", Art = EnergieflussKnotenart.Senke },
                new EnergieflussBildKnoten { Kennung = "einsp", Spalte = 6, Name = "Einspeisung BHKW", Art = EnergieflussKnotenart.Senke },
                new EnergieflussBildKnoten { Kennung = "rest", Spalte = 6, Name = "nicht zugeordnet", Art = EnergieflussKnotenart.Senke },
                new EnergieflussBildKnoten { Kennung = "einsam", Spalte = 3, Name = "ohne Band" }
            };
            var baender = new List<EnergieflussBildBand>
            {
                new EnergieflussBildBand { Von = "netz", Nach = "strom", Mwh = 40.0, Rolle = Farbrolle.STROM_NETZ,
                                           Zeigertext = "Netzbezug → Strom: 40,0 MWh/a (67 % des Stroms)" },
                new EnergieflussBildBand { Von = "gas", Nach = "bhkw", Mwh = 30.0, Rolle = Farbrolle.BRENNSTOFF },
                new EnergieflussBildBand { Von = "bhkw", Nach = "strom", Mwh = 20.0, Rolle = Farbrolle.STROM_BHKW },
                new EnergieflussBildBand { Von = "bhkw", Nach = "v_bhkw", Mwh = 10.0, Art = EnergieflussBandart.Verlust },
                new EnergieflussBildBand { Von = "strom", Nach = "bedarf", Mwh = 60.0 - klein - 5.0 - 0.001, Rolle = Farbrolle.VERBRAUCH_GESAMT },
                new EnergieflussBildBand { Von = "strom", Nach = "batterie", Mwh = 5.0, Rolle = Farbrolle.SPEICHERLADUNG },
                new EnergieflussBildBand { Von = "batterie", Nach = "bedarf", Mwh = 4.5, Rolle = Farbrolle.STROM_SPEICHER },
                new EnergieflussBildBand { Von = "batterie", Nach = "v_batterie", Mwh = 0.5, Rolle = Farbrolle.VERLUST, Art = EnergieflussBandart.Verlust },
                new EnergieflussBildBand { Von = "strom", Nach = "einsp", Mwh = klein, Rolle = Farbrolle.UEBERSCHUSS },
                new EnergieflussBildBand { Von = "strom", Nach = "rest", Mwh = 0.001, Art = EnergieflussBandart.NichtZugeordnet },
                // Was wegfallen muss: nicht endlich, negativ, null, unbekannter Knoten, Schleife.
                new EnergieflussBildBand { Von = "netz", Nach = "bedarf", Mwh = double.NaN },
                new EnergieflussBildBand { Von = "netz", Nach = "bedarf", Mwh = double.PositiveInfinity },
                new EnergieflussBildBand { Von = "netz", Nach = "bedarf", Mwh = -3.0 },
                new EnergieflussBildBand { Von = "netz", Nach = "bedarf", Mwh = 0.0 },
                new EnergieflussBildBand { Von = "netz", Nach = "gibtsnicht", Mwh = 3.0 },
                new EnergieflussBildBand { Von = "strom", Nach = "strom", Mwh = 3.0 }
            };
            return new EnergieflussBildDaten
            {
                Knoten = knoten, Baender = baender,
                Spaltentitel = new[] { "Quellen", "Wandler", "Strom", "Wandler", "Wärme · Kälte", "Speicher", "Bedarf" }
            };
        }

        private static List<Zeichenbefehl> Unter(Zeichenmodell m, string marke)
            => m.Befehle.Where(b => b.Marke == marke).ToList();

        private static Rechteck Knoten(Zeichenmodell m, string id) => Unter(m, "knoten:" + id).OfType<Rechteck>().Single();

        private static Pfad Band(Zeichenmodell m, string von, string nach) => Unter(m, "fluss:" + von + ":" + nach).OfType<Pfad>().Single();

        private static IEnumerable<string> Texte(Zeichenmodell m) => m.Befehle.OfType<Text>().Select(t => t.Inhalt);

        private static float Dicke(Pfad p) => p.Punkte[p.Punkte.Count - 1].Y - p.Punkte[0].Y;

        // =====================================================================
        // Maße und Fassungen
        // =====================================================================

        [Fact]
        public void Ohne_Mass_breit_sonst_schmal_und_Bericht_in_ihrem_Mass()
        {
            Zeichenmodell breit = ChartRenderer.EnergieflussModell(Gaben());
            Assert.Equal((1180, 560), (breit.Breite, breit.Hoehe));
            Zeichenmodell schmal = ChartRenderer.EnergieflussModell(Gaben(), ChartRenderer.ENERGIEFLUSS_SCHMAL);
            Assert.Equal((680, 640), (schmal.Breite, schmal.Hoehe));
            Zeichenmodell bericht = ChartRenderer.EnergieflussModell(Gaben(), ChartRenderer.ENERGIEFLUSS_BERICHT, true);
            Assert.Equal((1240, 620), (bericht.Breite, bericht.Hoehe));
            Assert.Null(breit.Flaeche);
            Assert.Empty(breit.Reihen);
        }

        [Fact]
        public void Schmal_nimmt_Kurznamen_breit_den_Namen()
        {
            Assert.Contains("Netzbezug", Texte(ChartRenderer.EnergieflussModell(Gaben())));
            List<string> schmal = Texte(ChartRenderer.EnergieflussModell(Gaben(), ChartRenderer.ENERGIEFLUSS_SCHMAL)).ToList();
            Assert.Contains("Netz", schmal);
            Assert.DoesNotContain("Netzbezug", schmal);
            // Ohne Kurzname bleibt der Name.
            Assert.Contains("Erdgas", schmal);
        }

        // =====================================================================
        // Layout
        // =====================================================================

        [Fact]
        public void Knoten_stehen_in_ihrer_Spalte_von_links_nach_rechts_und_in_Eingabereihenfolge()
        {
            Zeichenmodell m = ChartRenderer.EnergieflussModell(Gaben());
            Rechteck netz = Knoten(m, "netz"), gas = Knoten(m, "gas"), bhkw = Knoten(m, "bhkw"), strom = Knoten(m, "strom"),
                     batterie = Knoten(m, "batterie"), bedarf = Knoten(m, "bedarf"), einsp = Knoten(m, "einsp");
            Assert.Equal(netz.X, gas.X);
            Assert.True(netz.Y + netz.Hoehe < gas.Y, "Erdgas steht unter dem Netzbezug");
            Assert.True(netz.X < bhkw.X && bhkw.X < strom.X && strom.X < batterie.X && batterie.X < bedarf.X);
            Assert.Equal(bedarf.X, einsp.X);
            Assert.True(bedarf.Y < einsp.Y);
            Assert.All(new[] { netz, gas, bhkw, strom, batterie, bedarf }, k => Assert.Equal(KB, k.Breite));
            // Alles im Bild.
            foreach (Rechteck r in m.Befehle.OfType<Rechteck>())
                Assert.True(r.X >= 0 && r.Y >= 0 && r.X + r.Breite <= m.Breite && r.Y + r.Hoehe <= m.Hoehe, r.Marke);
        }

        [Fact]
        public void Verlustknoten_steht_in_der_Luecke_unter_dem_Fuss_seines_Erzeugers()
        {
            Zeichenmodell m = ChartRenderer.EnergieflussModell(Gaben());
            Rechteck bhkw = Knoten(m, "bhkw"), verlust = Knoten(m, "v_bhkw"), strom = Knoten(m, "strom");
            Assert.True(verlust.X > bhkw.X + KB && verlust.X + verlust.Breite < strom.X, "in der Lücke");
            Assert.True(verlust.Y >= bhkw.Y + bhkw.Hoehe, "unter dem Fuß");
            Assert.Equal(new[] { Farbrolle.VERLUST }, Rollen(m, "knoten:v_bhkw"));
            Assert.Equal(new[] { Farbrolle.VERLUST }, Rollen(m, "fluss:bhkw:v_bhkw"));
        }

        [Fact]
        public void Baender_schliessen_an_ihre_Knoten_an_und_fuellen_sie_luckenlos()
        {
            Zeichenmodell m = ChartRenderer.EnergieflussModell(Gaben());
            var baender = m.Befehle.OfType<Pfad>().Where(p => p.Marke.StartsWith("fluss:", StringComparison.Ordinal)).ToList();
            Assert.Equal(10, baender.Count);
            foreach (Pfad p in baender)
            {
                string[] teile = p.Marke.Split(':');
                Rechteck von = Knoten(m, teile[1]), nach = Knoten(m, teile[2]);
                int n = ChartRenderer.EF_STUETZSTELLEN;
                Assert.True(p.Geschlossen);
                Assert.Equal(2 * n, p.Punkte.Count);
                Assert.Equal(von.X + KB, p.Punkte[0].X, 3);
                Assert.Equal(nach.X, p.Punkte[n - 1].X, 3);
                Assert.Equal(von.X + KB, p.Punkte[2 * n - 1].X, 3);
                // Oberkante und Unterkante liegen an beiden Enden im Knoten.
                Assert.InRange(p.Punkte[0].Y, von.Y - 0.01f, von.Y + von.Hoehe + 0.01f);
                Assert.InRange(p.Punkte[2 * n - 1].Y, von.Y - 0.01f, von.Y + von.Hoehe + 0.01f);
                Assert.InRange(p.Punkte[n - 1].Y, nach.Y - 0.01f, nach.Y + nach.Hoehe + 0.01f);
                Assert.InRange(p.Punkte[n].Y, nach.Y - 0.01f, nach.Y + nach.Hoehe + 0.01f);
                // Dieselbe Dicke an beiden Enden.
                Assert.Equal(p.Punkte[n].Y - p.Punkte[n - 1].Y, Dicke(p), 3);
            }

            // Am Bus schließen die Zuflüsse oben an und liegen lückenlos übereinander.
            Rechteck strom = Knoten(m, "strom");
            Pfad netz = Band(m, "netz", "strom"), bhkw = Band(m, "bhkw", "strom");
            int s = ChartRenderer.EF_STUETZSTELLEN;
            Assert.Equal(strom.Y, netz.Punkte[s - 1].Y, 3);
            Assert.Equal(netz.Punkte[s].Y, bhkw.Punkte[s - 1].Y, 3);
            // Der Abfluss ist durch das Mindestband um höchstens dessen Breite höher als der Zufluss.
            Assert.InRange(strom.Y + strom.Hoehe - bhkw.Punkte[s].Y, -0.01f, ChartRenderer.EF_MINDESTBAND + 0.01f);
        }

        [Fact]
        public void Ein_kleiner_Fluss_wird_mit_Mindestbreite_gezeichnet()
        {
            Zeichenmodell m = ChartRenderer.EnergieflussModell(Gaben());
            Assert.Equal(ChartRenderer.EF_MINDESTBAND, Dicke(Band(m, "strom", "rest")), 3);
            Assert.True(Dicke(Band(m, "netz", "strom")) > 2 * Dicke(Band(m, "bhkw", "strom")) * 0.99f);
        }

        [Fact]
        public void Nicht_zugeordnet_traegt_einen_gestrichelten_Rand_in_REST()
        {
            Zeichenmodell m = ChartRenderer.EnergieflussModell(Gaben());
            Pfad rest = Band(m, "strom", "rest");
            Assert.NotNull(rest.Rand);
            Assert.NotNull(rest.Rand.Muster);
            Assert.Equal(Farbrolle.REST, rest.Rand.Ton.Rolle);
            Assert.Null(Band(m, "netz", "strom").Rand);
        }

        [Fact]
        public void Nicht_endliche_und_ungueltige_Baender_und_Knoten_ohne_Band_fallen_weg()
        {
            Zeichenmodell m = ChartRenderer.EnergieflussModell(Gaben());
            Assert.Empty(Unter(m, "fluss:netz:bedarf"));
            Assert.Empty(Unter(m, "fluss:netz:gibtsnicht"));
            Assert.Empty(Unter(m, "fluss:strom:strom"));
            Assert.Empty(Unter(m, "knoten:einsam"));
            Assert.Equal(10, m.Befehle.Count(b => b.Marke != null && b.Marke.StartsWith("knoten:", StringComparison.Ordinal)
                                                  && b is Rechteck));
        }

        [Fact]
        public void Ohne_Gaben_ein_leeres_Bild_ohne_Ausnahme()
        {
            Assert.Empty(ChartRenderer.EnergieflussModell(null).Befehle);
            Assert.Empty(ChartRenderer.EnergieflussModell(new EnergieflussBildDaten()).Befehle);
            Assert.NotEmpty(ChartRenderer.EnergieflussBild(null));
        }

        // =====================================================================
        // Beschriftung und Zeiger (SK-Q12)
        // =====================================================================

        [Fact]
        public void Bildschirmfassung_traegt_Namen_aber_keine_Zahl()
        {
            foreach (Bildmass mass in new[] { ChartRenderer.ENERGIEFLUSS_BREIT, ChartRenderer.ENERGIEFLUSS_SCHMAL })
            {
                Zeichenmodell m = ChartRenderer.EnergieflussModell(Gaben(), mass);
                List<string> texte = Texte(m).ToList();
                Assert.Contains("Strom", texte);
                Assert.Contains("Strombedarf", texte);
                Assert.DoesNotContain(texte, t => t.Any(char.IsDigit));
            }
        }

        [Fact]
        public void Berichtsfassung_nennt_die_Jahressumme_an_den_Knoten_und_keine_Zahl_an_den_Baendern()
        {
            Zeichenmodell m = ChartRenderer.EnergieflussModell(Gaben(), ChartRenderer.ENERGIEFLUSS_BERICHT, true);
            string zahl(double w) => w.ToString("N1", BerichtTexte.Kultur);
            List<string> texte = Texte(m).ToList();
            Assert.Contains(zahl(40.0) + " MWh/a", texte);                // Quelle: Wert unter dem Namen
            Assert.Contains("Strom · " + zahl(60.0), texte);               // Bus: Name · Wert über dem Knoten
            Assert.Contains(zahl(60.0 - 6.0 - 5.0 - 0.001 + 4.5) + " MWh/a", texte);   // Senke: das Größere aus Zu- und Abfluss
            // An den Bändern steht kein Text.
            Assert.DoesNotContain(m.Befehle, b => b is Text && b.Marke != null && b.Marke.StartsWith("fluss:", StringComparison.Ordinal));
            // Eine gepflegte Jahressumme gilt vor der Bandsumme.
            EnergieflussBildDaten d = Gaben();
            d.Knoten.Single(k => k.Kennung == "netz").Mwh = 41.25;
            Assert.Contains(zahl(41.25) + " MWh/a", Texte(ChartRenderer.EnergieflussModell(d, ChartRenderer.ENERGIEFLUSS_BERICHT, true)));
        }

        [Fact]
        public void Jedes_Band_und_jeder_Knoten_traegt_Marke_und_Zeigertext()
        {
            Zeichenmodell m = ChartRenderer.EnergieflussModell(Gaben());
            var markiert = m.Befehle.Where(b => b.Marke != null &&
                                                (b.Marke.StartsWith("fluss:", StringComparison.Ordinal) ||
                                                 b.Marke.StartsWith("knoten:", StringComparison.Ordinal))).ToList();
            Assert.NotEmpty(markiert);
            Assert.All(markiert, b => Assert.False(string.IsNullOrEmpty(b.Wert), b.Marke));

            // Gegebene Zeigertexte gelten wörtlich, fehlende bildet der Renderer aus Namen und Wert.
            Assert.Equal("Netzbezug → Strom: 40,0 MWh/a (67 % des Stroms)", Band(m, "netz", "strom").Wert);
            Assert.Equal("Strom: Zufluss 60,0 · Abfluss 60,0 MWh/a", Knoten(m, "strom").Wert);
            string zahl(double w) => w.ToString("N1", BerichtTexte.Kultur);
            Assert.Equal("Erdgas → BHKW: " + zahl(30.0) + " MWh/a", Band(m, "gas", "bhkw").Wert);
            Assert.Equal("BHKW: " + zahl(30.0) + " MWh/a", Knoten(m, "bhkw").Wert);

            // Das SVG trägt den Wert als data-wert am Band — die Zeigerzeile von DiagrammSvg liest ihn dort.
            SvgKnoten baum = SvgSchreiber.Baum(m);
            Assert.Contains(baum.Alle(), k => k.Attribute.Any(a => a.Key == "data-marke" && a.Value == "fluss:netz:strom") &&
                                              k.Attribute.Any(a => a.Key == "data-wert" && a.Value == Band(m, "netz", "strom").Wert));
        }

        // =====================================================================
        // Rollen und Determinismus
        // =====================================================================

        [Fact]
        public void Baender_und_Knoten_tragen_Rollen_statt_Farbwerten()
        {
            Zeichenmodell m = ChartRenderer.EnergieflussModell(Gaben());
            Assert.Equal(new[] { Farbrolle.STROM_NETZ }, Rollen(m, "fluss:netz:strom"));
            Assert.Equal(new[] { Farbrolle.BRENNSTOFF }, Rollen(m, "fluss:gas:bhkw"));
            Assert.Equal(new[] { Farbrolle.REST }, Rollen(m, "fluss:strom:rest"));
            Assert.Equal(new[] { Farbrolle.KNOTEN }, Rollen(m, "knoten:strom"));
            foreach (Zeichenbefehl b in m.Befehle)
                foreach (Farbton t in Toene(b)) Assert.Null(t.Fest);
            Pfad netz = Band(m, "netz", "strom");
            Assert.Equal(ChartRenderer.EF_BANDDECKUNG, netz.Fuellung.Ton.Deckung);
        }

        [Fact]
        public void Zweimal_gezeichnet_ist_byte_gleich()
        {
            Assert.True(ChartRenderer.EnergieflussModell(Gaben()).Gleicht(ChartRenderer.EnergieflussModell(Gaben())));
            Assert.Equal(ChartRenderer.EnergieflussBild(Gaben(), ChartRenderer.ENERGIEFLUSS_BERICHT, true),
                         ChartRenderer.EnergieflussBild(Gaben(), ChartRenderer.ENERGIEFLUSS_BERICHT, true));
        }

        // ---------------------------------------------------------------------

        /// <summary>Die Rollen unter der Marke ohne Beschriftung (<c>TEXT</c>) und Knotenrand (<c>RAHMEN</c>).</summary>
        internal static Farbrolle[] Rollen(Zeichenmodell m, string marke)
            => m.Befehle.Where(b => b.Marke == marke).SelectMany(Toene).Select(t => t.Rolle)
                .Where(r => r != Farbrolle.TEXT && r != Farbrolle.RAHMEN).Distinct().ToArray();

        private static IEnumerable<Farbton> Toene(Zeichenbefehl b)
        {
            switch (b)
            {
                case Rechteck r:
                    if (r.Rand != null) yield return r.Rand.Ton;
                    if (r.Fuellung != null) yield return r.Fuellung.Ton;
                    break;
                case Pfad p:
                    if (p.Rand != null) yield return p.Rand.Ton;
                    if (p.Fuellung != null) yield return p.Fuellung.Ton;
                    break;
                case Text t:
                    yield return t.Ton;
                    break;
            }
        }
    }
}
