using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Das Zeichenmodell des Energieflussbilds</b> (Konzept Energieflussbild 3 und 6, Schritte SK-2 und SK-2c, Variante A):
    /// Spalte aus dem längsten Weg zur Senke, Spuren (Strom oben, Wärme unten), Wegpunkte für Bänder über mehrere Spalten,
    /// Wandler als beschriftete Kästen, Verluste als Abzweige ohne Knotenbalken, Legende, Mindestbandbreite, breite, schmale
    /// und Berichtsfassung, Zeigertexte an jedem Band und Knoten, keine Zahl im Bildschirmbild (SK-Q12), Rollen statt
    /// Farbwerten, Kreuzungszahl, Determinismus.
    ///
    /// <para>Die Gaben sind synthetisch: Netz, Erdgas und Umweltwärme, ein BHKW mit Verlust, der Strombus, eine Wärmepumpe,
    /// der Wärmebus mit Speicherverlust, vier Senken, eine davon „nicht zugeordnet“. Die Klasse steht in der Sammlung
    /// „Testdatenbank“, weil das PNG gegen <c>Farbpalette.Aktuell</c> malt — prozessweiter Zustand, den die Farbtests setzen.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class EnergieflussModellTests
    {
        private const float KB = ChartRenderer.EF_KNOTENBREITE;
        private const int N = ChartRenderer.EF_STUETZSTELLEN;

        private static EnergieflussBildDaten Gaben(double klein = 6.0)
        {
            var knoten = new List<EnergieflussBildKnoten>
            {
                new EnergieflussBildKnoten { Kennung = "netz", Spur = EnergieflussSpur.Strom, Name = "Netzbezug", Kurzname = "Netz", Art = EnergieflussKnotenart.Quelle },
                new EnergieflussBildKnoten { Kennung = "gas", Spur = EnergieflussSpur.Gemischt, Name = "Erdgas", Art = EnergieflussKnotenart.Quelle },
                new EnergieflussBildKnoten { Kennung = "umwelt", Spur = EnergieflussSpur.Waerme, Name = "Umweltwärme", Kurzname = "Umwelt", Art = EnergieflussKnotenart.Quelle },
                new EnergieflussBildKnoten { Kennung = "bhkw", Spur = EnergieflussSpur.Gemischt, Name = "Blockheizkraftwerk", Kurzname = "BHKW", Art = EnergieflussKnotenart.Wandler },
                new EnergieflussBildKnoten { Kennung = "v_bhkw", Spur = EnergieflussSpur.Gemischt, Name = "Verlust", Art = EnergieflussKnotenart.Verlust },
                new EnergieflussBildKnoten { Kennung = "strom", Spur = EnergieflussSpur.Strom, Name = "Strom", Art = EnergieflussKnotenart.Bus,
                                             Zeigertext = "Strom: Zufluss 60,0 · Abfluss 60,0 MWh/a" },
                new EnergieflussBildKnoten { Kennung = "wp", Spur = EnergieflussSpur.Waerme, Name = "Wärmepumpe (Heizen)", Kurzname = "WP Heizen", Art = EnergieflussKnotenart.Wandler },
                new EnergieflussBildKnoten { Kennung = "waerme", Spur = EnergieflussSpur.Waerme, Name = "Wärme · Speicher", Kurzname = "Wärme", Art = EnergieflussKnotenart.Bus },
                new EnergieflussBildKnoten { Kennung = "v_waerme", Spur = EnergieflussSpur.Waerme, Name = "Speicherverlust", Art = EnergieflussKnotenart.Verlust },
                new EnergieflussBildKnoten { Kennung = "bedarf", Spur = EnergieflussSpur.Strom, Name = "Strombedarf", Art = EnergieflussKnotenart.Senke },
                new EnergieflussBildKnoten { Kennung = "einsp", Spur = EnergieflussSpur.Strom, Name = "Einspeisung BHKW", Art = EnergieflussKnotenart.Senke },
                new EnergieflussBildKnoten { Kennung = "rest", Spur = EnergieflussSpur.Strom, Name = "nicht zugeordnet", Art = EnergieflussKnotenart.Senke },
                new EnergieflussBildKnoten { Kennung = "heizung", Spur = EnergieflussSpur.Waerme, Name = "Heizung", Art = EnergieflussKnotenart.Senke },
                new EnergieflussBildKnoten { Kennung = "einsam", Spur = EnergieflussSpur.Waerme, Name = "ohne Band" }
            };
            var baender = new List<EnergieflussBildBand>
            {
                new EnergieflussBildBand { Von = "netz", Nach = "strom", Mwh = 40.0, Rolle = Farbrolle.STROM,
                                           Zeigertext = "Netzbezug → Strom: 40,0 MWh/a (67 % des Stroms)" },
                new EnergieflussBildBand { Von = "gas", Nach = "bhkw", Mwh = 30.0, Rolle = Farbrolle.BRENNSTOFF },
                new EnergieflussBildBand { Von = "bhkw", Nach = "strom", Mwh = 20.0, Rolle = Farbrolle.STROM },
                new EnergieflussBildBand { Von = "bhkw", Nach = "v_bhkw", Mwh = 10.0, Art = EnergieflussBandart.Verlust },
                new EnergieflussBildBand { Von = "strom", Nach = "wp", Mwh = 8.0, Rolle = Farbrolle.STROM },
                new EnergieflussBildBand { Von = "umwelt", Nach = "wp", Mwh = 24.0, Rolle = Farbrolle.UMWELTWAERME },
                new EnergieflussBildBand { Von = "wp", Nach = "waerme", Mwh = 32.0, Rolle = Farbrolle.WAERME },
                new EnergieflussBildBand { Von = "waerme", Nach = "heizung", Mwh = 31.5, Rolle = Farbrolle.WAERME },
                new EnergieflussBildBand { Von = "waerme", Nach = "v_waerme", Mwh = 0.5, Rolle = Farbrolle.VERLUST, Art = EnergieflussBandart.Verlust },
                new EnergieflussBildBand { Von = "strom", Nach = "bedarf", Mwh = 60.0 - klein - 8.0 - 0.001, Rolle = Farbrolle.STROM },
                new EnergieflussBildBand { Von = "strom", Nach = "einsp", Mwh = klein, Rolle = Farbrolle.STROM },
                new EnergieflussBildBand { Von = "strom", Nach = "rest", Mwh = 0.001, Art = EnergieflussBandart.NichtZugeordnet },
                // Was wegfallen muss: nicht endlich, negativ, null, unbekannter Knoten, Schleife, aus einem Verlustknoten.
                new EnergieflussBildBand { Von = "netz", Nach = "bedarf", Mwh = double.NaN },
                new EnergieflussBildBand { Von = "netz", Nach = "bedarf", Mwh = double.PositiveInfinity },
                new EnergieflussBildBand { Von = "netz", Nach = "bedarf", Mwh = -3.0 },
                new EnergieflussBildBand { Von = "netz", Nach = "bedarf", Mwh = 0.0 },
                new EnergieflussBildBand { Von = "netz", Nach = "gibtsnicht", Mwh = 3.0 },
                new EnergieflussBildBand { Von = "strom", Nach = "strom", Mwh = 3.0 },
                new EnergieflussBildBand { Von = "v_bhkw", Nach = "heizung", Mwh = 3.0 }
            };
            var legende = new List<EnergieflussBildLegende>
            {
                new EnergieflussBildLegende { Rolle = Farbrolle.STROM, Text = "Strom" },
                new EnergieflussBildLegende { Rolle = Farbrolle.WAERME, Text = "Wärme" },
                new EnergieflussBildLegende { Rolle = Farbrolle.BRENNSTOFF, Text = "Brennstoff" },
                new EnergieflussBildLegende { Rolle = Farbrolle.UMWELTWAERME, Text = "Umweltwärme" },
                new EnergieflussBildLegende { Rolle = Farbrolle.VERLUST, Text = "Verlust" },
                new EnergieflussBildLegende { Rolle = Farbrolle.REST, Text = "nicht zugeordnet", Gestrichelt = true }
            };
            return new EnergieflussBildDaten { Knoten = knoten, Baender = baender, Legende = legende };
        }

        private static List<Zeichenbefehl> Unter(Zeichenmodell m, string marke)
            => m.Befehle.Where(b => b.Marke == marke).ToList();

        private static Rechteck Knoten(Zeichenmodell m, string id) => Unter(m, "knoten:" + id).OfType<Rechteck>().Single();

        private static Pfad Band(Zeichenmodell m, string von, string nach) => Unter(m, "fluss:" + von + ":" + nach).OfType<Pfad>().Single();

        private static IEnumerable<string> Texte(Zeichenmodell m) => m.Befehle.OfType<Text>().Select(t => t.Inhalt);

        private static float Dicke(Pfad p) => p.Punkte[p.Punkte.Count - 1].Y - p.Punkte[0].Y;

        /// <summary>Das Band schließt links an <paramref name="von"/> und rechts an <paramref name="nach"/> an.</summary>
        private static void SchliesstAn(Pfad p, Rechteck von, Rechteck nach)
        {
            int n = p.Punkte.Count / 2;
            Assert.True(p.Geschlossen);
            Assert.Equal(0, p.Punkte.Count % (2 * N));
            Assert.Equal(von.X + von.Breite, p.Punkte[0].X, 3);
            Assert.Equal(von.X + von.Breite, p.Punkte[2 * n - 1].X, 3);
            Assert.InRange(p.Punkte[0].Y, von.Y - 0.01f, von.Y + von.Hoehe + 0.01f);
            Assert.InRange(p.Punkte[2 * n - 1].Y, von.Y - 0.01f, von.Y + von.Hoehe + 0.01f);
            if (nach == null) return;
            Assert.Equal(nach.X, p.Punkte[n - 1].X, 3);
            Assert.InRange(p.Punkte[n - 1].Y, nach.Y - 0.01f, nach.Y + nach.Hoehe + 0.01f);
            Assert.InRange(p.Punkte[n].Y, nach.Y - 0.01f, nach.Y + nach.Hoehe + 0.01f);
            Assert.Equal(p.Punkte[n].Y - p.Punkte[n - 1].Y, Dicke(p), 3);
        }

        // =====================================================================
        // Maße und Fassungen
        // =====================================================================

        [Fact]
        public void Ohne_Mass_breit_sonst_schmal_und_Bericht_in_ihrem_Mass()
        {
            Zeichenmodell breit = ChartRenderer.EnergieflussModell(Gaben());
            Assert.Equal((1180, 460), (breit.Breite, breit.Hoehe));
            Zeichenmodell schmal = ChartRenderer.EnergieflussModell(Gaben(), ChartRenderer.ENERGIEFLUSS_SCHMAL);
            Assert.Equal((680, 560), (schmal.Breite, schmal.Hoehe));
            Zeichenmodell bericht = ChartRenderer.EnergieflussModell(Gaben(), ChartRenderer.ENERGIEFLUSS_BERICHT, true);
            Assert.Equal((1240, 490), (bericht.Breite, bericht.Hoehe));
            Assert.Null(breit.Flaeche);
            Assert.Empty(breit.Reihen);
        }

        [Fact]
        public void Schmal_nimmt_Kurznamen_breit_den_Namen_Kaesten_immer_die_Kurzform()
        {
            List<string> breit = Texte(ChartRenderer.EnergieflussModell(Gaben())).ToList();
            Assert.Contains("Netzbezug", breit);
            Assert.Contains("WP Heizen", breit);          // der Kasten trägt die Kurzform
            Assert.DoesNotContain("Wärmepumpe (Heizen)", breit);
            List<string> schmal = Texte(ChartRenderer.EnergieflussModell(Gaben(), ChartRenderer.ENERGIEFLUSS_SCHMAL)).ToList();
            Assert.Contains("Netz", schmal);
            Assert.DoesNotContain("Netzbezug", schmal);
            Assert.Contains("Erdgas", schmal);            // ohne Kurzname bleibt der Name
        }

        // =====================================================================
        // Layout: Spalten, Spuren, Wegpunkte
        // =====================================================================

        [Fact]
        public void Spalte_ist_der_laengste_Weg_zur_Senke_Senken_in_der_letzten_Spalte()
        {
            Zeichenmodell m = ChartRenderer.EnergieflussModell(Gaben());
            Rechteck netz = Knoten(m, "netz"), gas = Knoten(m, "gas"), umwelt = Knoten(m, "umwelt"), bhkw = Knoten(m, "bhkw"),
                     strom = Knoten(m, "strom"), wp = Knoten(m, "wp"), waerme = Knoten(m, "waerme"),
                     bedarf = Knoten(m, "bedarf"), einsp = Knoten(m, "einsp"), heizung = Knoten(m, "heizung");
            // Erdgas → BHKW → Strom → Wärmepumpe → Wärme → Heizung: sechs Spalten; der Netzbezug steht direkt vor dem
            // Strombus (Spalte des BHKW), die Umweltwärme direkt vor der Wärmepumpe (Spalte des Strombusses).
            Assert.True(gas.X < bhkw.X && bhkw.X < strom.X && strom.X < wp.X && wp.X < waerme.X && waerme.X < heizung.X);
            Assert.Equal(bhkw.X + bhkw.Breite / 2f, netz.X + netz.Breite / 2f, 1);
            Assert.Equal(strom.X + strom.Breite / 2f, umwelt.X + umwelt.Breite / 2f, 1);
            Assert.Equal(bedarf.X, einsp.X);
            Assert.Equal(bedarf.X, heizung.X);
            // Spuren: Strom oben, Wärme unten — in der Senkenspalte und bei den Quellen.
            Assert.True(bedarf.Y + bedarf.Hoehe < heizung.Y && einsp.Y + einsp.Hoehe < heizung.Y, "Stromsenken über der Heizung");
            Assert.True(netz.Y + netz.Hoehe < bhkw.Y, "Netzbezug (Strom) über dem BHKW (Gemischt)");
            Assert.True(strom.Y + strom.Hoehe < umwelt.Y, "Strombus über der Umweltwärme");
            Assert.All(new[] { netz, gas, umwelt, strom, waerme, bedarf, einsp, heizung }, k => Assert.Equal(KB, k.Breite));
            foreach (Rechteck r in m.Befehle.OfType<Rechteck>())
                Assert.True(r.X >= 0 && r.Y >= 0 && r.X + r.Breite <= m.Breite && r.Y + r.Hoehe <= m.Hoehe, r.Marke);
        }

        [Fact]
        public void Ein_Band_ueber_mehrere_Spalten_laeuft_in_seiner_Spur_nicht_hinter_Kaesten()
        {
            Zeichenmodell m = ChartRenderer.EnergieflussModell(Gaben());
            Pfad bedarf = Band(m, "strom", "bedarf");
            // Strom → Strombedarf spannt drei Lücken: drei Teilstücke, ein geschlossener Pfad.
            Assert.Equal(2 * N * 3, bedarf.Punkte.Count);
            Rechteck wp = Knoten(m, "wp"), waerme = Knoten(m, "waerme");
            foreach (Punkt p in bedarf.Punkte)
            {
                Assert.False(p.X > wp.X && p.X < wp.X + wp.Breite && p.Y > wp.Y && p.Y < wp.Y + wp.Hoehe, "durch die Wärmepumpe");
                Assert.False(p.X > waerme.X && p.X < waerme.X + waerme.Breite && p.Y > waerme.Y && p.Y < waerme.Y + waerme.Hoehe, "durch den Wärmebus");
            }
            Assert.True(bedarf.Punkte.All(p => p.Y < wp.Y), "das Stromband läuft über der Wärmespur");
            Assert.Equal(2 * N, Band(m, "netz", "strom").Punkte.Count);
        }

        [Fact]
        public void Baender_schliessen_an_ihre_Knoten_an_und_fuellen_sie_lueckenlos()
        {
            Zeichenmodell m = ChartRenderer.EnergieflussModell(Gaben());
            var baender = m.Befehle.OfType<Pfad>().Where(p => p.Marke.StartsWith("fluss:", StringComparison.Ordinal)).ToList();
            Assert.Equal(12, baender.Count);
            foreach (string marke in new[] { "netz:strom", "gas:bhkw", "bhkw:strom", "strom:wp", "umwelt:wp", "wp:waerme", "waerme:heizung", "strom:bedarf", "strom:einsp", "strom:rest" })
            {
                string[] t = marke.Split(':');
                SchliesstAn(Band(m, t[0], t[1]), Knoten(m, t[0]), Knoten(m, t[1]));
            }
            // Am Bus schließen die Zuflüsse nach der Lage ihrer Quelle an und liegen lückenlos übereinander.
            Rechteck strom = Knoten(m, "strom");
            Pfad netz = Band(m, "netz", "strom"), bhkw = Band(m, "bhkw", "strom");
            Assert.Equal(strom.Y, netz.Punkte[N - 1].Y, 3);
            Assert.Equal(netz.Punkte[N].Y, bhkw.Punkte[N - 1].Y, 3);
            Assert.InRange(strom.Y + strom.Hoehe - bhkw.Punkte[N].Y, -0.01f, ChartRenderer.EF_MINDESTBAND + 0.01f);
        }

        [Fact]
        public void Wandler_sind_beschriftete_Kaesten_mit_Kurzname_in_Hintergrundfarbe()
        {
            Zeichenmodell m = ChartRenderer.EnergieflussModell(Gaben());
            Rechteck wp = Knoten(m, "wp"), bhkw = Knoten(m, "bhkw");
            Assert.True(wp.Breite > KB && bhkw.Breite > KB, "Kästen sind so breit wie ihre Beschriftung");
            Text wpText = Unter(m, "knoten:wp").OfType<Text>().Single();
            Assert.Equal("WP Heizen", wpText.Inhalt);
            Assert.Equal(Farbrolle.HINTERGRUND, wpText.Ton.Rolle);
            Assert.True(wpText.X > wp.X && wpText.X < wp.X + wp.Breite && wpText.Y > wp.Y && wpText.Y < wp.Y + wp.Hoehe, "im Kasten");
            Assert.Equal("BHKW", Unter(m, "knoten:bhkw").OfType<Text>().Single().Inhalt);
            // Busse: fetter Name über dem Balken, Quellen links, Senken rechts.
            Text stromText = Unter(m, "knoten:strom").OfType<Text>().Single();
            Assert.Equal("Strom", stromText.Inhalt);
            Assert.True(stromText.Y + stromText.Schrift.Punkt <= Knoten(m, "strom").Y + 1f, "über dem Bus");
            Assert.True(Unter(m, "knoten:netz").OfType<Text>().Single().X < Knoten(m, "netz").X, "Quelle links");
            Assert.True(Unter(m, "knoten:heizung").OfType<Text>().Single().X > Knoten(m, "heizung").X + KB, "Senke rechts");
        }

        [Fact]
        public void Verlust_ist_ein_kurzer_Abzweig_am_Erzeuger_ohne_Knotenbalken_mit_Beschriftung()
        {
            Zeichenmodell m = ChartRenderer.EnergieflussModell(Gaben());
            Assert.Empty(Unter(m, "knoten:v_bhkw"));
            Assert.Empty(Unter(m, "knoten:v_waerme"));
            Rechteck bhkw = Knoten(m, "bhkw"), strom = Knoten(m, "strom");
            Pfad v = Band(m, "bhkw", "v_bhkw");
            SchliesstAn(v, bhkw, null);
            float ende = v.Punkte.Max(p => p.X);
            Assert.True(ende > bhkw.X + bhkw.Breite && ende < strom.X, "kurz, in der Lücke");
            Assert.True(v.Punkte[N - 1].Y > v.Punkte[0].Y, "nach unten");
            Text t = Unter(m, "fluss:bhkw:v_bhkw").OfType<Text>().Single();
            Assert.Equal("Verlust", t.Inhalt);
            Assert.True(t.Y >= v.Punkte[N - 1].Y, "Beschriftung unter dem Ende");
            Assert.Equal(new[] { Farbrolle.VERLUST }, Rollen(m, "fluss:bhkw:v_bhkw"));
            Assert.Equal("Speicherverlust", Unter(m, "fluss:waerme:v_waerme").OfType<Text>().Single().Inhalt);
        }

        [Fact]
        public void Ein_kleiner_Fluss_wird_mit_Mindestbreite_gezeichnet()
        {
            Zeichenmodell m = ChartRenderer.EnergieflussModell(Gaben());
            Assert.Equal(ChartRenderer.EF_MINDESTBAND, Dicke(Band(m, "strom", "rest")), 3);
            Assert.True(Dicke(Band(m, "netz", "strom")) > 2 * Dicke(Band(m, "bhkw", "strom")) * 0.99f);
        }

        [Fact]
        public void Nicht_zugeordnet_traegt_einen_gestrichelten_Rand_in_REST_andere_den_weissen_Saum()
        {
            Zeichenmodell m = ChartRenderer.EnergieflussModell(Gaben());
            Pfad rest = Band(m, "strom", "rest");
            Assert.NotNull(rest.Rand);
            Assert.NotNull(rest.Rand.Muster);
            Assert.Equal(Farbrolle.REST, rest.Rand.Ton.Rolle);
            Pfad netz = Band(m, "netz", "strom");
            Assert.Equal(Farbrolle.HINTERGRUND, netz.Rand.Ton.Rolle);
            Assert.Null(netz.Rand.Muster);
            Assert.Equal(ChartRenderer.EF_SAUM, netz.Rand.Breite, 3);
        }

        [Fact]
        public void Nicht_endliche_und_ungueltige_Baender_und_Knoten_ohne_Band_fallen_weg()
        {
            Zeichenmodell m = ChartRenderer.EnergieflussModell(Gaben());
            Assert.Empty(Unter(m, "fluss:netz:bedarf"));
            Assert.Empty(Unter(m, "fluss:netz:gibtsnicht"));
            Assert.Empty(Unter(m, "fluss:strom:strom"));
            Assert.Empty(Unter(m, "fluss:v_bhkw:heizung"));
            Assert.Empty(Unter(m, "knoten:einsam"));
            Assert.Equal(11, m.Befehle.Count(b => b.Marke != null && b.Marke.StartsWith("knoten:", StringComparison.Ordinal) && b is Rechteck));
        }

        [Fact]
        public void Ohne_Gaben_ein_leeres_Bild_ohne_Ausnahme()
        {
            Assert.Empty(ChartRenderer.EnergieflussModell(null).Befehle);
            Assert.Empty(ChartRenderer.EnergieflussModell(new EnergieflussBildDaten()).Befehle);
            Assert.NotEmpty(ChartRenderer.EnergieflussBild(null));
            Assert.Equal(0, ChartRenderer.EnergieflussKreuzungen((Zeichenmodell)null));
        }

        [Fact]
        public void Die_Kette_der_Gaben_ist_kreuzungsfrei()
        {
            Assert.Equal(0, ChartRenderer.EnergieflussKreuzungen(Gaben()));
            Assert.Equal(0, ChartRenderer.EnergieflussKreuzungen(Gaben(), ChartRenderer.ENERGIEFLUSS_SCHMAL));
            Assert.Equal(0, ChartRenderer.EnergieflussKreuzungen(Gaben(), ChartRenderer.ENERGIEFLUSS_BERICHT, true));
        }

        // =====================================================================
        // Beschriftung, Legende und Zeiger (SK-Q12)
        // =====================================================================

        [Fact]
        public void Bildschirmfassung_traegt_Namen_und_Legende_aber_keine_Zahl()
        {
            foreach (Bildmass mass in new[] { ChartRenderer.ENERGIEFLUSS_BREIT, ChartRenderer.ENERGIEFLUSS_SCHMAL })
            {
                Zeichenmodell m = ChartRenderer.EnergieflussModell(Gaben(), mass);
                List<string> texte = Texte(m).ToList();
                Assert.Contains("Strom", texte);
                Assert.Contains("Strombedarf", texte);
                Assert.Contains("Brennstoff", texte);
                Assert.DoesNotContain(texte, t => t.Any(char.IsDigit));
            }
        }

        [Fact]
        public void Legende_steht_unter_dem_Bild_mit_Farbfeld_je_Energieform()
        {
            Zeichenmodell m = ChartRenderer.EnergieflussModell(Gaben());
            foreach (string f in new[] { "strom", "waerme", "brennstoff", "umweltwaerme", "verlust", "rest" })
            {
                Rechteck feld = Unter(m, "legende:" + f).OfType<Rechteck>().Single();
                Text text = Unter(m, "legende:" + f).OfType<Text>().Single();
                Assert.True(feld.Y >= m.Hoehe - ChartRenderer.EF_LEGENDE_HOEHE, f + " unter dem Bild");
                Assert.True(text.X > feld.X + feld.Breite);
            }
            Assert.Equal(Farbrolle.STROM, Unter(m, "legende:strom").OfType<Rechteck>().Single().Fuellung.Ton.Rolle);
            Assert.NotNull(Unter(m, "legende:rest").OfType<Rechteck>().Single().Rand.Muster);
            Assert.Equal("nicht zugeordnet", Unter(m, "legende:rest").OfType<Text>().Single().Inhalt);
            // Die Legende folgt der Reihenfolge der Gaben von links nach rechts.
            Assert.True(Unter(m, "legende:strom").OfType<Rechteck>().Single().X < Unter(m, "legende:verlust").OfType<Rechteck>().Single().X);
            // Ohne Legende kein Eintrag.
            EnergieflussBildDaten d = Gaben();
            d.Legende = Array.Empty<EnergieflussBildLegende>();
            Assert.DoesNotContain(ChartRenderer.EnergieflussModell(d).Befehle, b => b.Marke != null && b.Marke.StartsWith("legende:", StringComparison.Ordinal));
        }

        [Fact]
        public void Berichtsfassung_nennt_die_Jahressumme_an_Knoten_Kaesten_und_Verlusten_und_keine_Zahl_an_den_Baendern()
        {
            Zeichenmodell m = ChartRenderer.EnergieflussModell(Gaben(), ChartRenderer.ENERGIEFLUSS_BERICHT, true);
            string zahl(double w) => w.ToString("N1", BerichtTexte.Kultur);
            List<string> texte = Texte(m).ToList();
            Assert.Contains(zahl(40.0) + " MWh/a", texte);                 // Quelle: Wert unter dem Namen
            Assert.Contains("Strom · " + zahl(60.0), texte);                // Bus: Name · Wert über dem Balken
            Assert.Contains("WP Heizen · " + zahl(32.0), texte);            // Kasten: Kurzname · Wert
            Assert.Contains("Verlust " + zahl(10.0), texte);                // Verlustabzweig
            Assert.Contains(zahl(60.0 - 6.0 - 8.0 - 0.001) + " MWh/a", texte);   // Senke
            // An den Bändern steht keine Zahl — nur die Verlustabzweige tragen ihre Beschriftung.
            Assert.DoesNotContain(m.Befehle, b => b is Text && b.Marke != null && b.Marke.StartsWith("fluss:", StringComparison.Ordinal)
                                                  && !b.Marke.Contains(":v_"));
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
            Assert.Equal("Erdgas → Blockheizkraftwerk: " + zahl(30.0) + " MWh/a", Band(m, "gas", "bhkw").Wert);
            Assert.Equal("Blockheizkraftwerk: " + zahl(30.0) + " MWh/a", Knoten(m, "bhkw").Wert);
            Assert.Equal("Blockheizkraftwerk → Verlust: " + zahl(10.0) + " MWh/a", Band(m, "bhkw", "v_bhkw").Wert);

            // Das SVG trägt den Wert als data-wert am Band — die Zeigerzeile von DiagrammSvg liest ihn dort.
            SvgKnoten baum = SvgSchreiber.Baum(m);
            Assert.Contains(baum.Alle(), k => k.Attribute.Any(a => a.Key == "data-marke" && a.Value == "fluss:netz:strom") &&
                                              k.Attribute.Any(a => a.Key == "data-wert" && a.Value == Band(m, "netz", "strom").Wert));
        }

        // =====================================================================
        // Rollen und Determinismus
        // =====================================================================

        [Fact]
        public void Baender_und_Knoten_tragen_Rollen_statt_Farbwerten_in_zwei_Toenungen()
        {
            Zeichenmodell m = ChartRenderer.EnergieflussModell(Gaben());
            Assert.Equal(new[] { Farbrolle.STROM }, Rollen(m, "fluss:netz:strom"));
            Assert.Equal(new[] { Farbrolle.BRENNSTOFF }, Rollen(m, "fluss:gas:bhkw"));
            Assert.Equal(new[] { Farbrolle.REST }, Rollen(m, "fluss:strom:rest"));
            Assert.Equal(new[] { Farbrolle.KNOTEN }, Rollen(m, "knoten:strom"));
            Assert.Equal(new[] { Farbrolle.KNOTEN }, Rollen(m, "knoten:wp"));
            foreach (Zeichenbefehl b in m.Befehle)
                foreach (Farbton t in Toene(b)) Assert.Null(t.Fest);
            // Zwei Bänder in den Strombus: Grundton und hellere Tönung.
            byte[] deckungen = { Band(m, "netz", "strom").Fuellung.Ton.Deckung.Value, Band(m, "bhkw", "strom").Fuellung.Ton.Deckung.Value };
            Assert.Contains(ChartRenderer.EF_BANDDECKUNG, deckungen);
            Assert.Contains(ChartRenderer.EF_BANDDECKUNG_HELL, deckungen);
            // Quellen und Senken heller als Busse und Kästen.
            Assert.Equal(ChartRenderer.EF_BALKENDECKUNG, Knoten(m, "netz").Fuellung.Ton.Deckung);
            Assert.Null(Knoten(m, "strom").Fuellung.Ton.Deckung);
        }

        // =====================================================================
        // Stufen, Verbinder, schmale Fassung, Rückfall, Hinweis (SK-2d)
        // =====================================================================

        private static EnergieflussBildDaten MitTiteln(EnergieflussBildDaten d)
        {
            d.Stufentitel = new[] { "Einsatz", "Anlagen", "Energieform", "Nutzen" };
            return d;
        }

        /// <summary>Die Pfade unter der Marke eines Bandes — bei einem Verbinder drei (Stummel am Bus, Pfeilspitze, Stummel am Gerät).</summary>
        private static List<Pfad> Pfade(Zeichenmodell m, string von, string nach) => Unter(m, "fluss:" + von + ":" + nach).OfType<Pfad>().ToList();

        /// <summary>Die Zahl der Spalten: verschiedene Mitten der Knotenbalken und Kästen.</summary>
        private static int Spaltenzahl(Zeichenmodell m)
            => m.Befehle.OfType<Rechteck>().Where(r => r.Marke != null && r.Marke.StartsWith("knoten:", StringComparison.Ordinal))
                .Select(r => (float)Math.Round(r.X + r.Breite / 2f)).Distinct().Count();

        [Fact]
        public void Darstellung_Kette_ist_die_Vorgabe()
        {
            Assert.True(ChartRenderer.EnergieflussModell(Gaben()).Gleicht(
                ChartRenderer.EnergieflussModell(Gaben(), null, false, EnergieflussDarstellung.Kette)));
            Assert.Equal(6, Spaltenzahl(ChartRenderer.EnergieflussModell(Gaben())));
        }

        [Fact]
        public void Stufen_hat_vier_feste_Spalten_mit_Titeln_Quellen_Wandler_Busse_Senken()
        {
            Zeichenmodell m = ChartRenderer.EnergieflussModell(MitTiteln(Gaben()), null, false, EnergieflussDarstellung.Stufen);
            Rechteck bhkw = Knoten(m, "bhkw"), wp = Knoten(m, "wp");
            float xQ = Knoten(m, "netz").X, xW = bhkw.X + bhkw.Breite / 2f, xB = Knoten(m, "strom").X, xS = Knoten(m, "bedarf").X;
            Assert.True(xQ < xW && xW < xB && xB < xS);
            Assert.Equal(xQ, Knoten(m, "gas").X);
            Assert.Equal(xQ, Knoten(m, "umwelt").X);
            Assert.Equal(xW, wp.X + wp.Breite / 2f, 1);
            Assert.Equal(xB, Knoten(m, "waerme").X);
            foreach (string s in new[] { "einsp", "rest", "heizung" }) Assert.Equal(xS, Knoten(m, s).X);
            Assert.Equal(4, Spaltenzahl(m));
            // Titel über den Spalten unter der Marke stufe:<Spalte>, von links nach rechts.
            List<Text> titel = Enumerable.Range(0, 4).Select(c => Unter(m, "stufe:" + c).OfType<Text>().Single()).ToList();
            Assert.Equal(new[] { "Einsatz", "Anlagen", "Energieform", "Nutzen" }, titel.Select(t => t.Inhalt));
            Assert.All(titel, t => Assert.True(t.Y < Knoten(m, "netz").Y));
            Assert.True(titel[0].X < titel[1].X && titel[1].X < titel[2].X && titel[2].X < titel[3].X);
            // Netzbezug → Strom läuft über die Anlagenspalte hinweg und bleibt ein Pfad; nichts kreuzt sich.
            Assert.Equal(2 * 2 * N, Band(m, "netz", "strom").Punkte.Count);
            Assert.Equal(0, ChartRenderer.EnergieflussKreuzungen(m));
            // Ohne Titel in den Bilddaten keine Titel; die Kette zeichnet nie welche.
            Assert.DoesNotContain(ChartRenderer.EnergieflussModell(Gaben(), null, false, EnergieflussDarstellung.Stufen).Befehle, b => b.Marke != null && b.Marke.StartsWith("stufe:", StringComparison.Ordinal));
            Assert.DoesNotContain(ChartRenderer.EnergieflussModell(MitTiteln(Gaben())).Befehle, b => b.Marke != null && b.Marke.StartsWith("stufe:", StringComparison.Ordinal));
            Assert.True(m.Gleicht(ChartRenderer.EnergieflussModell(MitTiteln(Gaben()), null, false, EnergieflussDarstellung.Stufen)));
        }

        [Fact]
        public void Verbinder_Strom_an_Waermepumpe_sind_zwei_Stummel_unter_einer_Marke_mit_Pfeil_Kreis_und_Beschriftung()
        {
            Zeichenmodell m = ChartRenderer.EnergieflussModell(Gaben(), null, false, EnergieflussDarstellung.Stufen);
            List<Zeichenbefehl> alle = Unter(m, "fluss:strom:wp");
            List<Pfad> pfade = alle.OfType<Pfad>().ToList();
            Rechteck strom = Knoten(m, "strom"), wp = Knoten(m, "wp");
            Assert.Equal(3, pfade.Count);
            Pfad aus = Assert.Single(pfade, p => p.Punkte.Count == 2 * N && Math.Abs(p.Punkte[0].X - (strom.X + strom.Breite)) < 0.01f);
            Pfad ein = Assert.Single(pfade, p => p.Punkte.Count == 2 * N && Math.Abs(p.Punkte[N - 1].X - wp.X) < 0.01f);
            Pfad pfeil = Assert.Single(pfade, p => p.Punkte.Count == 3);
            Assert.InRange(aus.Punkte[0].Y, strom.Y - 0.01f, strom.Y + strom.Hoehe + 0.01f);
            Assert.InRange(ein.Punkte[N - 1].Y, wp.Y - 0.01f, wp.Y + wp.Hoehe + 0.01f);
            Assert.True(aus.Punkte[N - 1].X - aus.Punkte[0].X <= ChartRenderer.EF_STUMMEL_MAX + 0.01f);
            Assert.Equal(aus.Punkte[N - 1].X, pfeil.Punkte[0].X, 2);
            Kreis kreis = Assert.Single(alle.OfType<Kreis>());
            Assert.True(kreis.X < ein.Punkte[0].X);
            Assert.Equal(Farbrolle.STROM, kreis.Rand.Ton.Rolle);
            Assert.Equal(new[] { "→ Wärmepumpe (Heizen)", "Strom" }, alle.OfType<Text>().Select(t => t.Inhalt));
            string zahl(double w) => w.ToString("N1", BerichtTexte.Kultur);
            Assert.All(alle, b => Assert.Equal("Strom → Wärmepumpe (Heizen): " + zahl(8.0) + " MWh/a", b.Wert));
            // Dasselbe Band ist in der breiten Kette ein Pfad; ein Verbinder zählt nie als Kreuzung.
            Assert.Single(Pfade(ChartRenderer.EnergieflussModell(Gaben()), "strom", "wp"));
            Assert.DoesNotContain(ChartRenderer.EnergieflussKreuzungspaare(m), p => p.A == "fluss:strom:wp" || p.B == "fluss:strom:wp");
            // Die Berichtsfassung nennt den Wert an beiden Stummeln; die schmale Fassung die Kurzform des Geräts.
            Zeichenmodell bericht = ChartRenderer.EnergieflussModell(Gaben(), ChartRenderer.ENERGIEFLUSS_BERICHT, true, EnergieflussDarstellung.Stufen);
            Assert.Equal(new[] { "→ Wärmepumpe (Heizen) " + zahl(8.0), "Strom " + zahl(8.0) }, Unter(bericht, "fluss:strom:wp").OfType<Text>().Select(t => t.Inhalt));
            Zeichenmodell schmal = ChartRenderer.EnergieflussModell(Gaben(), ChartRenderer.ENERGIEFLUSS_SCHMAL, false, EnergieflussDarstellung.Stufen);
            Assert.Equal(new[] { "→ WP Heizen", "Strom" }, Unter(schmal, "fluss:strom:wp").OfType<Text>().Select(t => t.Inhalt));
        }

        [Fact]
        public void Schmal_zeichnet_Strom_an_Wandler_als_Verbinder_und_die_Kette_wird_kuerzer()
        {
            Zeichenmodell schmal = ChartRenderer.EnergieflussModell(Gaben(), ChartRenderer.ENERGIEFLUSS_SCHMAL);
            Assert.Equal(3, Pfade(schmal, "strom", "wp").Count);
            Assert.Single(Pfade(schmal, "bhkw", "strom"));
            Assert.Single(Pfade(schmal, "gas", "bhkw"));
            // Ohne das Band Strom → Wärmepumpe im längsten Weg rückt der Strombus vor die Senken: vier statt sechs Spalten.
            Assert.Equal(4, Spaltenzahl(schmal));
            Assert.True(Knoten(schmal, "wp").X < Knoten(schmal, "strom").X);
            Assert.Equal(0, ChartRenderer.EnergieflussKreuzungen(schmal));
            Assert.DoesNotContain(schmal.Befehle, b => b.Marke != null && b.Marke.StartsWith("stufe:", StringComparison.Ordinal));
        }

        [Fact]
        public void Kette_faellt_ueber_sechs_Spalten_auf_die_Verbinderregel_zurueck()
        {
            // Ein Vorwärmer zwischen Erdgas und BHKW macht die Kette sieben Spalten lang; der Rückfall macht Strom → Wärmepumpe
            // zum Verbinder, und die Kette wird fünf Spalten kurz.
            EnergieflussBildDaten d = Gaben();
            List<EnergieflussBildKnoten> knoten = d.Knoten.ToList();
            knoten.Add(new EnergieflussBildKnoten { Kennung = "vor", Spur = EnergieflussSpur.Gemischt, Name = "Vorwärmer", Kurzname = "Vor", Art = EnergieflussKnotenart.Wandler });
            List<EnergieflussBildBand> baender = d.Baender.ToList();
            baender.Single(b => b.Von == "gas" && b.Nach == "bhkw").Nach = "vor";
            baender.Add(new EnergieflussBildBand { Von = "vor", Nach = "bhkw", Mwh = 30.0, Rolle = Farbrolle.BRENNSTOFF });
            d.Knoten = knoten;
            d.Baender = baender;
            Zeichenmodell m = ChartRenderer.EnergieflussModell(d);
            Assert.Equal(3, Pfade(m, "strom", "wp").Count);
            Assert.Single(Pfade(m, "vor", "bhkw"));
            Assert.Equal(5, Spaltenzahl(m));
            Assert.True(Spaltenzahl(m) <= ChartRenderer.EF_MAX_SPALTEN_KETTE);
        }

        [Fact]
        public void Leere_Bilddaten_mit_Hinweis_zeigen_nur_den_Hinweis()
        {
            Zeichenmodell m = ChartRenderer.EnergieflussModell(new EnergieflussBildDaten { Hinweis = "Kälte: keine Flüsse in dieser Version" });
            Zeichenbefehl einzig = Assert.Single(m.Befehle);
            Text t = Assert.IsType<Text>(einzig);
            Assert.Equal("hinweis", t.Marke);
            Assert.Equal("Kälte: keine Flüsse in dieser Version", t.Inhalt);
            Assert.InRange(t.X, 0f, m.Breite);
            Assert.NotEmpty(ChartRenderer.EnergieflussBild(new EnergieflussBildDaten { Hinweis = "leer" }, ChartRenderer.ENERGIEFLUSS_SCHMAL, false, EnergieflussDarstellung.Stufen));
        }

        [Fact]
        public void Zweimal_gezeichnet_ist_byte_gleich()
        {
            Assert.True(ChartRenderer.EnergieflussModell(Gaben()).Gleicht(ChartRenderer.EnergieflussModell(Gaben())));
            Assert.Equal(ChartRenderer.EnergieflussBild(Gaben(), ChartRenderer.ENERGIEFLUSS_BERICHT, true),
                         ChartRenderer.EnergieflussBild(Gaben(), ChartRenderer.ENERGIEFLUSS_BERICHT, true));
        }

        // ---------------------------------------------------------------------

        /// <summary>Die Rollen unter der Marke ohne Beschriftung (<c>TEXT</c>, <c>HINTERGRUND</c> der Kästen und des Saums) und Knotenrand (<c>RAHMEN</c>).</summary>
        internal static Farbrolle[] Rollen(Zeichenmodell m, string marke)
            => m.Befehle.Where(b => b.Marke == marke).SelectMany(Toene).Select(t => t.Rolle)
                .Where(r => r != Farbrolle.TEXT && r != Farbrolle.RAHMEN && r != Farbrolle.HINTERGRUND).Distinct().ToArray();

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
