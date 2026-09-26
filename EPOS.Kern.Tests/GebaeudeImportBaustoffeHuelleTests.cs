using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EPOS.UI.Dialoge.Import;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Abschnitt „Baustoffe" auf der Datenseite</b> (<see cref="GebaeudeImportHuelle"/>, Namensabgleich
    /// Welle 2) — ohne Datenbank: Ohne Projekt nimmt die Hülle Katalog und Synonyme aus der
    /// Auslieferungssaat im Speicher; jede Zuordnung des Dialogs liegt über den gemerkten und bildet den
    /// Vorschlag neu; das Ergebnis trägt die wirksamen Zuordnungen in die ausstehende Herkunft, auch
    /// ohne Bauteilschalter.
    /// </summary>
    [Collection("Testdatenbank")]   // ein Fall tauscht die Zugriffsschicht — prozessweiter Zustand
    public sealed class GebaeudeImportBaustoffeHuelleTests : IDisposable
    {
        private const string MATERIALHAUS = "ifc4_haus_materialnamen.ifc";
        private const int ZEMENTESTRICH = 5;

        private readonly Kulturvorrichtung _kultur = new();
        private readonly ITestOutputHelper _aus;

        public GebaeudeImportBaustoffeHuelleTests(ITestOutputHelper aus)
        {
            _aus = aus;
        }

        public void Dispose() => _kultur.Dispose();

        private static readonly IReadOnlyDictionary<string, bool> Keine = new Dictionary<string, bool>();

        private static Dictionary<string, int?> Zuordnungen(params (string Schluessel, int? Id)[] paare)
            => paare.ToDictionary(p => p.Schluessel, p => p.Id, StringComparer.Ordinal);

        /// <summary>Liest eine Probe über den Parametersatz einer Hülle (ohne Projekt, sofern nicht anders gebaut).</summary>
        private static async Task<GebaeudeImportHuelle> Gelesen(string probe, GebaeudeImportHuelle h = null)
        {
            h ??= new GebaeudeImportHuelle();
            var lesen = (Func<string, IProgress<GebaeudeImportFortschritt>, CancellationToken, Task<GebaeudeLesestand>>)h.Gaben()["Lesen"];
            GebaeudeLesestand gelesen = await lesen(GbxmlImportTests.Probe(probe), null, CancellationToken.None);
            Assert.True(gelesen.Gelesen, string.Join(" | ", gelesen.Meldungen.Select(m => m.Text)));
            return h;
        }

        private static GebaeudeImportStand Zuordnen(GebaeudeImportHuelle h, IReadOnlyDictionary<string, int?> zuordnungen = null)
            => h.Zuordnen(new GebaeudeZuordnungsanfrage(0, null, Keine, null, zuordnungen, h.Quelle.Zonenregel));

        private static GebaeudeMaterialzeileDaten Zeile(GebaeudeImportStand stand, string name)
            => Assert.Single(stand.Baustoffe!.Zeilen, z => z.Name == name);

        // =================================================================================
        //  Der Abschnitt aus der Saat
        // =================================================================================

        [Fact]
        public async Task Der_Abschnitt_zeigt_je_Materialname_Stufe_Baustoff_und_Werte()
        {
            GebaeudeImportHuelle h = await Gelesen(MATERIALHAUS);
            GebaeudeImportStand stand = Zuordnen(h);

            GebaeudeBaustoffeDaten b = Assert.IsType<GebaeudeBaustoffeDaten>(stand.Baustoffe);
            Assert.Equal(20, b.Zeilen.Count);
            Assert.Equal("16 von 20 zugeordnet, 1 ohne Treffer", b.Zusammenfassung);
            Assert.Equal(6, h.Vorschlag.Aufbauten.Count);

            GebaeudeMaterialzeileDaten fb = Zeile(stand, "Fußbodenaufbau");
            Assert.True(fb.OhneTreffer);
            Assert.Equal((GebaeudeAbgleichSchluessel.Ohne, "ohne Treffer"), (fb.AbgleichSchluessel, fb.Abgleich));
            Assert.Null(fb.IdBaustoff);
            Assert.Equal("", fb.Baustoff);
            Assert.Equal("fussbodenaufbau", fb.Schluessel);
            Assert.Equal("ohne vollständigen Aufbau", fb.Werte);
            Assert.False(fb.Gemerkt);
            Assert.False(fb.Vorgemerkt);

            GebaeudeMaterialzeileDaten gips = Zeile(stand, "Gipsputz");
            Assert.Equal(("N5", "Wortanfang", 2, 4), (gips.AbgleichSchluessel, gips.Abgleich, gips.IdBaustoff, gips.Schichten));
            Assert.Equal("aus dem Katalog", gips.Werte);
            Assert.StartsWith("λ 0,", gips.Stoffwerte, StringComparison.Ordinal);
            Assert.Contains("W/(mK)", gips.Stoffwerte, StringComparison.Ordinal);
            Assert.Contains("N5", gips.Beleg, StringComparison.Ordinal);
            Assert.False(gips.OhneTreffer);

            GebaeudeMaterialzeileDaten luft = Zeile(stand, "Air");
            Assert.Equal(("LUFTSCHICHT", "Luftschicht", "Widerstand nach DIN EN ISO 6946"), (luft.AbgleichSchluessel, luft.Abgleich, luft.Werte));
            Assert.Null(luft.IdBaustoff);
            Assert.Equal("verworfen", Assert.Single(b.Zeilen, z => z.Name.StartsWith("Solid", StringComparison.Ordinal)).Abgleich);
            Assert.Equal("entfällt", Assert.Single(b.Zeilen, z => z.Name.StartsWith("Solid", StringComparison.Ordinal)).Werte);
            Assert.Contains(b.Zeilen, z => z.AbgleichSchluessel == "N3" && z.Abgleich == "genauer Name");
            Assert.Contains(b.Zeilen, z => z.AbgleichSchluessel == "N4" && z.Abgleich == "Synonym");

            // Die Klappliste: der ganze Katalog der Saat, gruppiert; je Gruppe neutral vor Hersteller.
            Assert.Equal(BaustoffSchema.Saat.Count, b.Katalog.Sum(g => g.Eintraege.Count));
            Assert.Equal(b.Katalog.Count, b.Katalog.Select(g => g.Titel).Distinct().Count());
            foreach (GebaeudeBaustoffgruppe g in b.Katalog)
            {
                List<bool> hersteller = g.Eintraege.Select(e => BaustoffSchema.SaatZu(e.Id).Hersteller != null).ToList();
                Assert.True(hersteller.SequenceEqual(hersteller.OrderBy(x => x)), g.Titel + ": Herstellerzeilen vor neutralen");
            }
            GebaeudeBaustoffwahl mitHersteller = b.Katalog.SelectMany(g => g.Eintraege).First(e => BaustoffSchema.SaatZu(e.Id).Hersteller != null);
            Assert.EndsWith("(" + BaustoffSchema.SaatZu(mitHersteller.Id).Hersteller + ")", mitHersteller.Text, StringComparison.Ordinal);
            Assert.Equal(1, b.Katalog[0].Eintraege[0].Id);
        }

        [Fact]
        public async Task Ohne_Materialnamen_steht_kein_Abschnitt_und_bei_vollen_Werten_sagt_ihn_die_Zusammenfassung()
        {
            GebaeudeImportStand ohneKonstruktionen = Zuordnen(await Gelesen("gbxml_ohne_konstruktionen.xml"));
            Assert.Null(ohneKonstruktionen.Baustoffe);

            GebaeudeImportStand gbxml = Zuordnen(await Gelesen("gbxml_haus_si.xml"));
            GebaeudeBaustoffeDaten b = Assert.IsType<GebaeudeBaustoffeDaten>(gbxml.Baustoffe);
            Assert.StartsWith(b.Zeilen.Count + " Materialnamen, alle Stoffwerte aus der Datei", b.Zusammenfassung, StringComparison.Ordinal);
            Assert.All(b.Zeilen, z => Assert.Equal("aus der Datei", z.Werte));
            Assert.All(b.Zeilen, z => Assert.False(z.OhneTreffer));
        }

        // =================================================================================
        //  Zuordnungen des Dialogs überlagern den Abgleich; Neubildung
        // =================================================================================

        [Fact]
        public async Task Eine_Zuordnung_des_Dialogs_ueberlagert_den_Abgleich_und_bildet_den_Vorschlag_neu()
        {
            GebaeudeImportHuelle h = await Gelesen(MATERIALHAUS);
            GebaeudeImportStand vorher = Zuordnen(h);
            Assert.Equal(6, h.Vorschlag.Aufbauten.Count);
            Assert.Null(h.Vorschlag.Zeilen.Single(z => z.Bauteil.Bezeichner == "Kellerdecke").Bauteil.ID_Aufbau);

            // „Fußbodenaufbau" → Zementestrich: die Kellerdecke bekommt ihren Aufbau, kein Name bleibt ohne Treffer.
            GebaeudeImportStand mit = Zuordnen(h, Zuordnungen(("fussbodenaufbau", ZEMENTESTRICH)));
            Assert.Equal(7, h.Vorschlag.Aufbauten.Count);
            Assert.NotNull(h.Vorschlag.Zeilen.Single(z => z.Bauteil.Bezeichner == "Kellerdecke").Bauteil.ID_Aufbau);
            GebaeudeMaterialzeileDaten fb = Zeile(mit, "Fußbodenaufbau");
            Assert.Equal((GebaeudeAbgleichSchluessel.EigeneZuordnung, "eigene Zuordnung", (int?)ZEMENTESTRICH), (fb.AbgleichSchluessel, fb.Abgleich, fb.IdBaustoff));
            Assert.Equal(BaustoffSchema.SaatZu(ZEMENTESTRICH).Bezeichner, fb.Baustoff);
            Assert.Equal("aus dem Katalog", fb.Werte);
            Assert.True(fb.Vorgemerkt);
            Assert.False(fb.Gemerkt);
            Assert.False(fb.OhneTreffer);
            Assert.Equal("17 von 20 zugeordnet, 0 ohne Treffer", mit.Baustoffe!.Zusammenfassung);
            Assert.NotEqual(vorher.Bauteile!.Kopftext, mit.Bauteile!.Kopftext);
            Assert.Contains("7", mit.Bauteile.Kopftext, StringComparison.Ordinal);

            // Ein automatischer Treffer lässt sich überstimmen (N7 vor N5).
            GebaeudeImportStand anders = Zuordnen(h, Zuordnungen(("gipsputz", 1)));
            GebaeudeMaterialzeileDaten gips = Zeile(anders, "Gipsputz");
            Assert.Equal((GebaeudeAbgleichSchluessel.EigeneZuordnung, (int?)1), (gips.AbgleichSchluessel, gips.IdBaustoff));

            // Ohne die Zuordnung (entfernt) ist der Vorschlag wieder der alte.
            GebaeudeImportStand zurueck = Zuordnen(h, Zuordnungen());
            Assert.Equal(6, h.Vorschlag.Aufbauten.Count);
            Assert.Equal(vorher.Baustoffe!.Zusammenfassung, zurueck.Baustoffe!.Zusammenfassung);
            Assert.True(Zeile(zurueck, "Fußbodenaufbau").OhneTreffer);
        }

        [Fact]
        public async Task Das_Ergebnis_traegt_die_wirksamen_Zuordnungen_in_die_Herkunft_auch_ohne_Bauteilschalter()
        {
            GebaeudeImportHuelle h = await Gelesen(MATERIALHAUS);
            GebaeudeImportStand stand = Zuordnen(h);
            Dictionary<string, int?> dialog = Zuordnungen(("Fußbodenaufbau", ZEMENTESTRICH), ("kein name der datei", 3), ("gipsputz", null));

            var mitZone = new GebaeudeImportErgebnis(0, 4, "Haus", Keine, stand.Zeilen.ToList(), true, dialog, h.Quelle.Zonenregel);
            Assert.NotNull(h.SatzAusErgebnis(mitZone));
            GebaeudeImportHerkunft herkunft = h.Herkunft;
            Assert.Equal<KeyValuePair<string, int?>>(new[] { new KeyValuePair<string, int?>("fussbodenaufbau", ZEMENTESTRICH) },
                                                     herkunft.Baustoffzuordnungen);
            Assert.Equal(7, herkunft.Vorschlag.Aufbauten.Count);

            // Ohne Bauteilschalter: kein Vorschlag, die Zuordnungen reisen trotzdem (sie gelten für das Projekt).
            h.SatzAusErgebnis(mitZone with { AlsZone = false });
            Assert.Null(h.Herkunft.Vorschlag);
            Assert.Equal(ZEMENTESTRICH, h.Herkunft.Baustoffzuordnungen["fussbodenaufbau"]);

            // Ohne Zuordnung keine.
            h.SatzAusErgebnis(mitZone with { Baustoffzuordnungen = null });
            Assert.Null(h.Herkunft.Baustoffzuordnungen);
        }

        [Fact]
        public async Task Gemerkte_und_vorgemerkte_Zuordnungen_gelten_und_lassen_sich_entfernen()
        {
            // Die gemerkte Zuordnung des Projekts (über eine eigene Quelle) gilt, die Zeile weiß es.
            var h = new GebaeudeImportHuelle
            {
                Abgleichsquelle = BaustoffabgleichDaten.AusSaat(new[] { new BaustoffNamenzuordnung("fussbodenaufbau", ZEMENTESTRICH) }),
            };
            await Gelesen(MATERIALHAUS, h);
            GebaeudeMaterialzeileDaten fb = Zeile(Zuordnen(h), "Fußbodenaufbau");
            Assert.Equal(GebaeudeAbgleichSchluessel.EigeneZuordnung, fb.AbgleichSchluessel);
            Assert.True(fb.Gemerkt);
            Assert.False(fb.Vorgemerkt);
            Assert.Equal(7, h.Vorschlag.Aufbauten.Count);

            // Entfernen: null nimmt sie weg — der Name ist wieder ohne Treffer, das Entfernen reist mit.
            Dictionary<string, int?> weg = Zuordnungen(("fussbodenaufbau", null));
            GebaeudeMaterialzeileDaten ohne = Zeile(Zuordnen(h, weg), "Fußbodenaufbau");
            Assert.True(ohne.OhneTreffer);
            Assert.True(ohne.Gemerkt);
            Assert.True(ohne.Vorgemerkt);
            Assert.Equal(6, h.Vorschlag.Aufbauten.Count);
            GebaeudeImportStand stand = Zuordnen(h, weg);
            h.SatzAusErgebnis(new GebaeudeImportErgebnis(0, 4, "Haus", Keine, stand.Zeilen.ToList(), false, weg, h.Quelle.Zonenregel));
            Assert.Equal<KeyValuePair<string, int?>>(new[] { new KeyValuePair<string, int?>("fussbodenaufbau", null) },
                                                     h.Herkunft.Baustoffzuordnungen);

            // Dieselbe Zuordnung noch einmal zu setzen ist keine Änderung.
            h.SatzAusErgebnis(new GebaeudeImportErgebnis(0, 4, "Haus", Keine, stand.Zeilen.ToList(), false,
                                                          Zuordnungen(("fussbodenaufbau", ZEMENTESTRICH)), h.Quelle.Zonenregel));
            Assert.Null(h.Herkunft.Baustoffzuordnungen);

            // Vorgemerkt aus einem anderen Import derselben Liste: gilt wie gemerkt.
            var zweite = new GebaeudeImportHuelle { Vorgemerkt = Zuordnungen(("fussbodenaufbau", ZEMENTESTRICH)) };
            await Gelesen(MATERIALHAUS, zweite);
            GebaeudeMaterialzeileDaten vor = Zeile(Zuordnen(zweite), "Fußbodenaufbau");
            Assert.Equal((GebaeudeAbgleichSchluessel.EigeneZuordnung, true), (vor.AbgleichSchluessel, vor.Gemerkt));
        }

        [Fact]
        public async Task Ohne_Projekt_bildet_die_Huelle_den_Abschnitt_ohne_Datenbank()
        {
            IDatenzugriff vorher = DataRepository.Zugriff;
            var zugriffe = new Zaehlzugriff(vorher);
            try
            {
                DataRepository.Zugriff = zugriffe;
                GebaeudeImportHuelle h = await Gelesen(MATERIALHAUS);
                GebaeudeImportStand stand = Zuordnen(h, Zuordnungen(("fussbodenaufbau", ZEMENTESTRICH)));
                Assert.Equal(20, stand.Baustoffe!.Zeilen.Count);
                var ergebnis = new GebaeudeImportErgebnis(0, 4, "Probe ohne Datenbank", Keine, stand.Zeilen.ToList(), true,
                                                          Zuordnungen(("fussbodenaufbau", ZEMENTESTRICH)));
                h.Pruefen(ergebnis);
                Assert.NotNull(h.Herkunft.Baustoffzuordnungen);
                Assert.Equal(0, zugriffe.Gesamt);
            }
            finally
            {
                DataRepository.Zugriff = vorher;
            }
        }

        [Fact]
        public async Task Die_Texte_des_Abschnitts_stehen_auch_englisch()
        {
            GebaeudeImportHuelle h = await Gelesen(MATERIALHAUS);
            using (new Kulturvorrichtung("en-US"))
            {
                GebaeudeImportStand stand = Zuordnen(h, Zuordnungen(("gipsputz", 1)));
                Assert.Equal("16 of 20 assigned, 1 without a match", stand.Baustoffe!.Zusammenfassung);
                Assert.Equal("no match", Zeile(stand, "Fußbodenaufbau").Abgleich);
                Assert.Equal("own assignment", Zeile(stand, "Gipsputz").Abgleich);
                Assert.Equal("from the catalogue", Zeile(stand, "Gipsputz").Werte);
                Assert.Equal("air layer", Zeile(stand, "Air").Abgleich);
                Assert.StartsWith("λ 1 W/(mK) · ρ 1,800 kg/m³", Zeile(stand, "Gipsputz").Stoffwerte, StringComparison.Ordinal);
            }
        }

        // =================================================================================
        //  Auskunft: was der Abschnitt je Probe zeigt
        // =================================================================================

        /// <summary>
        /// Zur Auskunft, nicht als Abnahme: je Probe die Zusammenfassung, die Namen ohne Treffer und die
        /// Aufbauten vor und nach einer Handzuordnung (jeder Name ohne Treffer → Zementestrich).
        /// </summary>
        [Theory]
        [InlineData(MATERIALHAUS)]
        [InlineData("gbxml_haus_si.xml")]
        [InlineData("ifc2x3_schichten.ifc")]
        public async Task Auskunft_der_Abschnitt_je_Probe(string probe)
        {
            GebaeudeImportHuelle h = await Gelesen(probe);
            GebaeudeImportStand vorher = Zuordnen(h);
            int aufbautenVorher = h.Vorschlag.Aufbauten.Count;
            GebaeudeBaustoffeDaten b = vorher.Baustoffe;
            List<GebaeudeMaterialzeileDaten> ohne = b?.Zeilen.Where(z => z.OhneTreffer).ToList() ?? new List<GebaeudeMaterialzeileDaten>();
            GebaeudeImportStand nachher = Zuordnen(h, ohne.ToDictionary(z => z.Schluessel, _ => (int?)ZEMENTESTRICH, StringComparer.Ordinal));
            _aus.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0}: {1} | ohne Treffer: {2} | Aufbauten vorher {3}, nach Handzuordnung {4} | {5}",
                probe, b?.Zusammenfassung ?? "(kein Abschnitt)", ohne.Count == 0 ? "keine" : string.Join(", ", ohne.Select(z => z.Name)),
                aufbautenVorher, h.Vorschlag.Aufbauten.Count, nachher.Baustoffe?.Zusammenfassung ?? ""));
            foreach (GebaeudeMaterialzeileDaten z in b?.Zeilen ?? Array.Empty<GebaeudeMaterialzeileDaten>())
                _aus.WriteLine("  " + z.Name + " · " + z.Schichten + " · " + z.Abgleich + " · " + z.Baustoff + " · " + z.Werte);
            Assert.True(h.Vorschlag.Aufbauten.Count >= aufbautenVorher);
        }
    }
}
