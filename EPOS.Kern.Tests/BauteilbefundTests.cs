using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Befund je Bauteil</b> (Abstimmung G5, B1; G5-3): der Körpergrund aus dem IFC-Leser an der Probe
    /// <c>ifc4_g5_befund.ifc</c> (<see cref="IfcProbenErzeuger"/>), bis in die Zeilen des Bauteilvorschlags getragen; die
    /// Regel rot vor orange; der Befund gespeicherter Bauteile; Texte und Summen.
    /// </summary>
    public sealed class BauteilbefundTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung("en-US");

        public void Dispose() => _kultur.Dispose();

        internal static GebaeudeImportAblauf Lesen(string datei)
        {
            string pfad = Path.Combine(IfcProbenTests.Ordner(), datei);
            var a = new GebaeudeImportAblauf();
            using (FileStream s = File.OpenRead(pfad))
                a.Lesen(s, pfad, new IfcImportProfil());
            Assert.NotNull(a.Abbild);
            return a;
        }

        private static IEnumerable<AbbildBauteil> Bauteile(GebaeudeImportAblauf a)
            => a.Abbild.Gebaeude.SelectMany(g => g.Bauteile).Concat(a.Abbild.BauteileOhneGebaeude);

        /// <summary>
        /// Je Wand der Probe der erwartete Körpergrund: Süd ein <c>IfcAdvancedBrep</c> (kein Träger lesbar), Teil eine Extrusion
        /// samt <c>IfcAdvancedBrep</c> (nur teilweise lesbar), Ost ein Beschnitt (nur der erste Operand), Nord eine offene
        /// Schale, Platte ein geschlossener Körper mit einer einzigen waagerechten Fläche (keine Wandseite: entartet), West
        /// eine schlichte Extrusion (lesbar).
        /// </summary>
        internal static readonly (string Name, Bauteilbefundgrund Grund)[] ERWARTET =
        {
            ("Wand Süd", Bauteilbefundgrund.DarstellungNichtLesbar),
            ("Wand Teil", Bauteilbefundgrund.DarstellungNichtLesbar),
            ("Wand Ost", Bauteilbefundgrund.OhneBeschnitt),
            ("Wand Nord", Bauteilbefundgrund.SchaleOffen),
            ("Wand Platte", Bauteilbefundgrund.KoerperEntartet),
            ("Wand West", Bauteilbefundgrund.Keiner),
        };

        [Fact]
        public void Der_IFC_Leser_bildet_den_Koerpergrund_je_Wand()
        {
            GebaeudeImportAblauf a = Lesen("ifc4_g5_befund.ifc");
            foreach ((string name, Bauteilbefundgrund grund) in ERWARTET)
            {
                AbbildBauteil b = Assert.Single(Bauteile(a), x => x.Name == name);
                Assert.True(grund == b.Koerpergrund, name + ": " + b.Koerpergrund);
                // Die Fläche kommt aus dem Mengensatz — auch bei unlesbarem Körper.
                Assert.Equal(Flaechenherkunft.Mengensatz, b.Flaechenherkunft);
            }
        }

        [Fact]
        public void Der_Koerpergrund_reicht_bis_in_die_Zeilen_des_Bauteilvorschlags()
        {
            GebaeudeImportAblauf a = Lesen("ifc4_g5_befund.ifc");
            // Klasse E: jede Wand hat ein U aus der Vorgabe — es bleibt allein der Körperbefund.
            GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.Bilden(a, 0, 'E');
            foreach ((string name, Bauteilbefundgrund grund) in ERWARTET)
            {
                AbbildBauteil b = Assert.Single(Bauteile(a), x => x.Name == name);
                GebaeudeBauteilzeile z = Assert.Single(v.Zeilen, x => x.Kennung == b.Kennung);
                Assert.True(z.Bauteil.U_Wert > 0.0, name);
                Assert.Equal(grund, z.Befundgrund);
                Assert.Equal(grund == Bauteilbefundgrund.Keiner ? Bauteilbefund.Ohne : Bauteilbefund.KoerperUnlesbar, z.Befund);
            }

            // Summen: fünf orange Wände (25 + 10 + 20 + 25 + 12 = 92 m²); grau die Westwand (20 m²) und die beiden Vorgabezeilen
            // ohne Quelle für Dach und Boden aus der Grundfläche (je 80 m²) — ohne Körper, mit U der Vorgabe; keine rote.
            List<GebaeudeBauteilzeile> vorgabe = v.Zeilen.Where(z => z.Kennung == null).ToList();
            Assert.Equal((2, 160.0), (vorgabe.Count, Math.Round(vorgabe.Sum(z => z.Bauteil.Flaeche), 6)));
            IReadOnlyList<Befundsumme> s = Bauteilbefunde.Summen(v.Zeilen);
            Assert.Equal(new[] { Bauteilbefund.Ohne, Bauteilbefund.KoerperUnlesbar, Bauteilbefund.OhneEigenschaften }, s.Select(x => x.Befund));
            Assert.Equal((3, 180.0), (s[0].Zahl, Math.Round(s[0].Flaeche_M2, 6)));
            Assert.Equal((5, 92.0), (s[1].Zahl, Math.Round(s[1].Flaeche_M2, 6)));
            Assert.Equal((0, 0.0), (s[2].Zahl, s[2].Flaeche_M2));
        }

        private static GebaeudeBauteilzeile Zeile(double flaeche, double? u = null)
            => new GebaeudeBauteilzeile(new BauteilModel { Bauteilart = "AUSSENWAND", Flaeche = flaeche, U_Wert = u },
                                        GebaeudeZielfelder.FLAECHE_AUSSENWAND, "IfcWall", "w-1", null);

        [Fact]
        public void Ohne_U_Wert_oder_ohne_Flaeche_ist_rot_und_geht_dem_Koerper_vor()
        {
            GebaeudeBauteilzeile z = Zeile(10.0);
            Assert.Equal(Bauteilbefundgrund.OhneUWert, z.Befundgrund);
            Assert.Equal(Bauteilbefund.OhneEigenschaften, z.Befund);

            // Vorrang: unlesbarer Körper und kein U → rot.
            z.Koerpergrund = Bauteilbefundgrund.OhneBeschnitt;
            Assert.Equal(Bauteilbefund.OhneEigenschaften, z.Befund);

            // Ein U (gleich woher), ein U aus Schichten oder ein Aufbau genügt — dann bleibt der Körperbefund.
            z.Bauteil.U_Wert = 0.3;
            Assert.Equal((Bauteilbefundgrund.OhneBeschnitt, Bauteilbefund.KoerperUnlesbar), (z.Befundgrund, z.Befund));
            GebaeudeBauteilzeile s = Zeile(10.0);
            s.USchichten = 0.25;
            Assert.Equal(Bauteilbefund.Ohne, s.Befund);
            GebaeudeBauteilzeile mitAufbau = Zeile(10.0);
            mitAufbau.Bauteil.ID_Aufbau = -1;
            Assert.Equal(Bauteilbefund.Ohne, mitAufbau.Befund);

            // Keine Fläche geht allem vor.
            GebaeudeBauteilzeile leer = Zeile(0.0, 0.3);
            leer.Koerpergrund = Bauteilbefundgrund.SchaleOffen;
            Assert.Equal((Bauteilbefundgrund.OhneFlaeche, Bauteilbefund.OhneEigenschaften), (leer.Befundgrund, leer.Befund));
        }

        [Fact]
        public void Ohne_Baualtersklasse_und_ohne_U_der_Datei_ist_jede_Wand_rot()
        {
            GebaeudeImportAblauf a = Lesen("ifc4_g5_befund.ifc");
            GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.Bilden(a, 0, null);
            List<GebaeudeBauteilzeile> waende = v.Zeilen.Where(z => z.Kennung != null).ToList();
            Assert.Equal(ERWARTET.Length, waende.Count);
            foreach (GebaeudeBauteilzeile z in waende)
                Assert.Equal((Bauteilbefundgrund.OhneUWert, Bauteilbefund.OhneEigenschaften), (z.Befundgrund, z.Befund));
        }

        [Fact]
        public void Gespeicherte_Bauteile_kennen_nur_den_Befund_ihrer_Spalten()
        {
            Assert.Equal(Bauteilbefundgrund.OhneUWert, Bauteilbefunde.Grund(new BauteilModel { Flaeche = 5.0 }));
            Assert.Equal(Bauteilbefundgrund.Keiner, Bauteilbefunde.Grund(new BauteilModel { Flaeche = 5.0, ID_Aufbau = 7 }));
            Assert.Equal(Bauteilbefundgrund.Keiner, Bauteilbefunde.Grund(new BauteilModel { Flaeche = 5.0, U_Wert = 0.2 }));
            Assert.Equal(Bauteilbefundgrund.OhneFlaeche, Bauteilbefunde.Grund(new BauteilModel { Flaeche = 0.0, U_Wert = 0.2 }));
        }

        [Fact]
        public void Koerpergrund_nach_Vermerk_und_Lesbarkeit()
        {
            var lesbar = new Dateikoerper();
            Assert.Equal(Bauteilbefundgrund.Keiner, Bauteilbefunde.Koerpergrund(false, null, Array.Empty<string>()));
            Assert.Equal(Bauteilbefundgrund.DarstellungNichtLesbar, Bauteilbefunde.Koerpergrund(true, null, Array.Empty<string>()));
            Assert.Equal(Bauteilbefundgrund.DarstellungNichtLesbar, Bauteilbefunde.Koerpergrund(true, lesbar, new[] { "IfcAdvancedBrep" }));
            Assert.Equal(Bauteilbefundgrund.Keiner, Bauteilbefunde.Koerpergrund(true, lesbar, Array.Empty<string>()));
            (Koerpervermerk[] Vermerke, Bauteilbefundgrund Grund)[] faelle =
            {
                (new[] { Koerpervermerk.Bogen, Koerpervermerk.Uneben, Koerpervermerk.Mehrschale }, Bauteilbefundgrund.Keiner),
                (new[] { Koerpervermerk.Loch }, Bauteilbefundgrund.LochNichtAngebunden),
                (new[] { Koerpervermerk.Offen, Koerpervermerk.Loch }, Bauteilbefundgrund.SchaleOffen),
                (new[] { Koerpervermerk.Offen, Koerpervermerk.OhneBeschnitt }, Bauteilbefundgrund.OhneBeschnitt),
            };
            foreach ((Koerpervermerk[] v, Bauteilbefundgrund g) in faelle)
                Assert.Equal(g, Bauteilbefunde.Koerpergrund(true, new Dateikoerper { Vermerke = v }, Array.Empty<string>()));
        }

        [Fact]
        public void Jeder_Grund_und_jeder_Befund_hat_einen_Text_in_beiden_Sprachen()
        {
            foreach (string kultur in new[] { "de-DE", "en-US" })
            {
                using (new Kulturvorrichtung(kultur))
                {
                    foreach (Bauteilbefundgrund g in Enum.GetValues<Bauteilbefundgrund>())
                        Assert.True((g == Bauteilbefundgrund.Keiner) == (Bauteilbefunde.Text(g).Length == 0), kultur + ": " + g);
                    var namen = Enum.GetValues<Bauteilbefund>().Select(Bauteilbefunde.Name).ToList();
                    Assert.All(namen, n => Assert.False(string.IsNullOrWhiteSpace(n)));
                    Assert.Equal(namen.Count, namen.Distinct().Count());
                }
            }
            using (new Kulturvorrichtung("de-DE"))
                Assert.Equal("Körper mit offener Schale.", Bauteilbefunde.Text(Bauteilbefundgrund.SchaleOffen));
            using (new Kulturvorrichtung("en-US"))
                Assert.Equal("Body with an open shell.", Bauteilbefunde.Text(Bauteilbefundgrund.SchaleOffen));
        }
    }
}
