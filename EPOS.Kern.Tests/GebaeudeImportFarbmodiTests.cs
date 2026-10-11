using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dialoge.Import;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Auftrag AB</b>: Farbmodi „Aufbau" und „Befund" der Gebäudeansicht im Import, lesbarer Bauteilname statt der Kennung
    /// und der benannte Grund, wenn kein Bauteilvorschlag vorliegt — je Format über die Importhülle.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class GebaeudeImportFarbmodiTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static async Task<(GebaeudeImportHuelle Huelle, Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand> Zuordnen)> Gelesen(string datei)
        {
            var h = new GebaeudeImportHuelle();
            var gaben = h.Gaben();
            var lesen = (Func<string, IProgress<GebaeudeImportFortschritt>, CancellationToken, Task<GebaeudeLesestand>>)gaben["Lesen"];
            GebaeudeLesestand st = await lesen(datei, null, CancellationToken.None);
            Assert.True(st.Gelesen);
            return (h, (Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>)gaben["Zuordnen"]);
        }

        private static GebaeudeZuordnungsanfrage Anfrage(string regel, IReadOnlyList<GebaeudePlanschritt> schritte = null)
            => new GebaeudeZuordnungsanfrage(0, null, new Dictionary<string, bool>(), null, null, regel, null, false, schritte, null);

        /// <summary>Je Raum mit Dateikörper: Dreiecke mit Bauteil, deren Bauteil eine Stufe hat.</summary>
        private static int Treffer(GebaeudeAnsichtDaten a)
            => a.Koerperraeume.Sum(k => k.Dreiecksbauteile?.Count(b => b != null && a.Bauteilstufen.ContainsKey(b)) ?? 0);

        [Theory]
        [InlineData("sqproj")]
        [InlineData("gbxml")]
        [InlineData("ifc")]
        public async Task Aufbau_und_Befund_faerben_auch_Koerper_aus_Flaechen(string format)
        {
            string datei = format switch
            {
                "sqproj" => SqprojProbenErzeuger.Geometriehaus().Schreiben(SqprojProbenErzeuger.TempPfad("ab")),
                "gbxml" => GbxmlImportTests.Probe("gbxml_g5_flaechen.xml"),
                _ => GbxmlImportTests.Probe("ifc4_koerper_bauteile.ifc"),
            };
            (_, Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand> zuordnen) = await Gelesen(datei);
            GebaeudeAnsichtDaten a = zuordnen(Anfrage("Z4")).Ansicht;
            Assert.True(a.HatDateikoerper, format);
            Assert.True(a.AufbauWaehlbar, format);
            Assert.True(a.BefundWaehlbar, format);
            Assert.True(Treffer(a) > 0, format);
            Assert.Equal("", a.Vorschlagsgrund);
            // Die Stufenbytes des Körperfelds: je Raum mit Bauteilen ein Abschnitt.
            Assert.Contains(a.Koerperfeld().Verzeichnis, e => e.StufenAb >= 0);
            Assert.Contains(a.Koerperfeld().Verzeichnis, e => e.BefundAb >= 0);
        }

        [Fact]
        public async Task Die_Flaechen_der_Projektdatei_tragen_lesbare_Namen_statt_der_Kennung()
        {
            (_, Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand> zuordnen) =
                await Gelesen(SqprojProbenErzeuger.Geometriehaus().Schreiben(SqprojProbenErzeuger.TempPfad("ab")));
            GebaeudeImportStand s = zuordnen(Anfrage("X3"));
            IReadOnlyList<GebaeudeFlaechenzeileDaten> flaechen = s.Zonierung.Flaechen;
            Assert.NotEmpty(flaechen);
            string aussenwand = BauteilaufbauCtrl.BauteilartText(DbWerte.BAUTEILART_AUSSENWAND);
            // Die Probe trägt keine Bauteilnamen: Art, Raum und Ausrichtung statt der Kennung.
            Assert.Contains(flaechen, f => f.Zeile.Bezeichner.StartsWith(aussenwand + " ", StringComparison.Ordinal));
            Assert.DoesNotContain(flaechen, f => f.Zeile.Bezeichner.StartsWith("{", StringComparison.Ordinal));
        }

        [Fact]
        public async Task Ohne_Vorschlag_nennt_die_Ansicht_den_Grund()
        {
            (_, Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand> zuordnen) =
                await Gelesen(SqprojProbenErzeuger.Geometriehaus().Schreiben(SqprojProbenErzeuger.TempPfad("ab")));
            GebaeudeImportStand erst = zuordnen(Anfrage("X3"));
            Assert.NotNull(erst.Zonierung.Plan);
            // Einen Raum aus seiner Zone nehmen: Er ist keiner Zone zugeordnet, der Plan sperrt den Vorschlag (Mehrzonenkonzept 6.4).
            string raum = erst.Zonierung.Plan.Zonen.SelectMany(z => z.Raumliste).Select(r => r.Kennung).First();
            var heraus = new GebaeudePlanschritt(GebaeudePlanschrittArt.ZUORDNEN, Raeume: new[] { raum }, Zone: null);
            GebaeudeImportStand s = zuordnen(Anfrage("X3", new[] { heraus }));
            Assert.NotEmpty(s.Zonierung.Plan.NichtZugeordnet);
            GebaeudeAnsichtDaten a = s.Ansicht;
            Assert.False(a.AufbauWaehlbar);
            Assert.False(a.BefundWaehlbar);
            Assert.NotEqual("", a.Vorschlagsgrund);
            Assert.Equal(s.Bauteile.Ablehnung, a.Vorschlagsgrund);
            Assert.Equal(0, a.DreieckeOhneBauteilbezug());
        }

        [Fact]
        public void Der_lesbare_Name_nimmt_CAD_Name_sonst_Art_Raum_und_Ausrichtung()
        {
            static string Raum(string k) => k == "r1" ? "EG-R4" : null;
            var mitName = new AbbildBauteil { Kennung = "{g-1}", Name = " Wand Süd ", Art = Bauteilart.Aussenwand };
            Assert.Equal("Wand Süd", GebaeudeBauteilvorschlag.Lesename(mitName, Raum));

            var ohneName = new AbbildBauteil { Kennung = "{g-2}", Art = Bauteilart.Aussenwand, AzimutGrad = 180, NeigungGrad = 90 };
            ohneName.Nachbarn.Add(new AbbildNachbar("r1", "innen"));
            string aw = BauteilaufbauCtrl.BauteilartText(DbWerte.BAUTEILART_AUSSENWAND);
            Assert.Equal(aw + " EG-R4 S", GebaeudeBauteilvorschlag.Lesename(ohneName, Raum));

            var grenze = new AbbildBauteil { Kennung = "{g-3}", Art = Bauteilart.Aussenwand, AzimutGrad = 90, NeigungGrad = 90 };
            grenze.Grenzen.Add(new AbbildGrenze { Kennung = "x", RaumKennung = "r1" });
            Assert.Equal(aw + " EG-R4 O", GebaeudeBauteilvorschlag.Lesename(grenze, Raum));

            // Waagerecht ohne Raum: weder Raum noch Ausrichtung — die Kennung bleibt.
            var decke = new AbbildBauteil { Kennung = "{g-4}", Art = Bauteilart.Decke, AzimutGrad = 0, NeigungGrad = 0 };
            Assert.Equal("{g-4}", GebaeudeBauteilvorschlag.Lesename(decke, Raum));
        }

        [Theory]
        [InlineData(0.0, 90.0, "N")]
        [InlineData(359.0, 90.0, "N")]
        [InlineData(45.0, 90.0, "NO")]
        [InlineData(180.0, 90.0, "S")]
        [InlineData(-90.0, 90.0, "W")]
        [InlineData(200.0, 0.0, null)]
        [InlineData(200.0, 180.0, null)]
        public void Die_Himmelsrichtung_in_acht_Sektoren(double azimut, double neigung, string erwartet)
            => Assert.Equal(erwartet, GebaeudeBauteilvorschlag.Himmelsrichtung(azimut, neigung));
    }
}
