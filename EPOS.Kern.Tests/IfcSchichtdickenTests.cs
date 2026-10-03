using System.Globalization;
using System.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Rückfall „Schichtdicke in Millimetern"</b> des IFC-Lesers: CAD-Exporte erklären <c>METRE</c> als
    /// Längeneinheit, schreiben <c>IfcMaterialLayer.LayerThickness</c> aber in Millimetern. Liegt eine Dicke
    /// eines Schichtsatzes über 1 m, gilt der ganze Satz als Millimeter; Schichten unter 0,5 mm (Folien) werden
    /// übergangen, Bleche ab 0,5 mm gehalten — beides mit Sammelhinweis je Datei. Probe: <c>ifc4_schichtdicken_mm.ifc</c>.
    /// </summary>
    public sealed class IfcSchichtdickenTests : System.IDisposable
    {
        private const string PROBE = "ifc4_schichtdicken_mm.ifc";
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        private static AbbildBauteil Bauteil(GebaeudeImportAblauf a, string aufbau)
            => Assert.Single(a.Abbild.Gebaeude[0].Bauteile, b => b.Aufbau?.Name == aufbau);

        [Fact]
        public void Ein_Satz_in_Millimetern_wird_umgerechnet_und_die_Folie_uebergangen()
        {
            GebaeudeImportAblauf a = BauteilvorschlagProbe.Lesen(PROBE);
            AbbildBauteil wand = Bauteil(a, "Außenwand Millimeter");
            // Die Folie (0,2 mm) liegt unter 0,5 mm und wird übergangen; das Blech (0,9 mm) bleibt mit seiner Masse.
            Assert.Equal(3, wand.Aufbau.Schichten.Count);
            Assert.Equal(new[] { "Blech", "Dämmung", "Beton" }, wand.Aufbau.Schichten.Select(s => s.Name));
            BauteilvorschlagProbe.Nah(0.0009, wand.Aufbau.Schichten[0].DickeM);
            BauteilvorschlagProbe.Nah(0.16, wand.Aufbau.Schichten[1].DickeM);
            BauteilvorschlagProbe.Nah(0.20, wand.Aufbau.Schichten[2].DickeM);
            BauteilvorschlagProbe.Nah(0.3609, wand.DickeM);

            PruefMeldung mm = Assert.Single(a.Abbild.Meldungen, m => m.Schluessel == "IMP_IFC_PROT_SCHICHTDICKE_MM");
            Assert.Equal(PruefStufe.Warnung, mm.Stufe);
            // Ein Sammelhinweis je Datei: ein Satz, größte Dicke 200 m nach der Dateieinheit, mit Satz und Name.
            Assert.Equal(new[] { "1", "200", wand.Aufbau.Kennung, "Außenwand Millimeter" }, mm.Werte);
            PruefMeldung duenn = Assert.Single(a.Abbild.Meldungen, m => m.Schluessel == "IMP_IFC_PROT_SCHICHT_DUENN");
            Assert.Equal(PruefStufe.Info, duenn.Stufe);
            Assert.Equal(new[] { "1", "Folie (0.2 mm) ×1" }, duenn.Werte);
            // Die Meldungen tragen einen Text in beiden Sprachen.
            foreach (string kultur in new[] { "de-DE", "en-US" })
                foreach (string schluessel in new[] { "IMP_IFC_PROT_SCHICHTDICKE_MM", "IMP_IFC_PROT_SCHICHT_DUENN" })
                    Assert.False(string.IsNullOrEmpty(WindowsFormsApplication1.MyResource.Resource.ResourceManager
                                                          .GetString(schluessel, new CultureInfo(kultur))), schluessel + " " + kultur);
        }

        [Fact]
        public void Gegenprobe_ein_Satz_in_Metern_bleibt_unveraendert()
        {
            GebaeudeImportAblauf a = BauteilvorschlagProbe.Lesen(PROBE);
            AbbildBauteil dach = Bauteil(a, "Dach Meter");
            Assert.Equal(2, dach.Aufbau.Schichten.Count);
            BauteilvorschlagProbe.Nah(0.2, dach.Aufbau.Schichten[0].DickeM);
            BauteilvorschlagProbe.Nah(0.016, dach.Aufbau.Schichten[1].DickeM);
            // Der Sammelhinweis zählt nur den Satz der Wand.
            PruefMeldung mm = Assert.Single(a.Abbild.Meldungen, m => m.Schluessel == "IMP_IFC_PROT_SCHICHTDICKE_MM");
            Assert.Equal("1", mm.Werte[0]);
            Assert.NotEqual(dach.Aufbau.Kennung, mm.Werte[2]);
        }

        [Fact]
        public void Der_Bauteilvorschlag_rechnet_beide_Aufbauten_aus_den_Schichten()
        {
            GebaeudeBauteilvorschlag v = BauteilvorschlagProbe.Vorschlag(PROBE, 0, 'E');
            Assert.False(v.Abgelehnt);
            Assert.Equal(2, v.Aufbauten.Count);
            // Wand und Dach rechnen aus den Schichten; die fehlende Grundfläche fällt auf ihre Vorgabe.
            GebaeudeBauteilzeile[] mitAufbau = v.Zeilen.Where(z => z.Bauteil.ID_Aufbau.HasValue).ToArray();
            Assert.Equal(2, mitAufbau.Length);
            foreach (GebaeudeBauteilzeile z in mitAufbau)
            {
                Assert.True(z.USchichten > 0.0, z.ToString());
                // Die Wand trägt Blech, Dämmung und Beton, das Dach Beton und Dämmung.
                int soll = z.Bauteil.Bauteilart == DbWerte.BAUTEILART_AUSSENWAND ? 3 : 2;
                Assert.Equal(soll, BauteilvorschlagProbe.Aufbau(v, z).Schichten.Count);
            }
            // U der Wand aus 0,9 mm Blech, 0,16 m Dämmung und 0,20 m Beton: 1/(0,13 + 0,0009/50 + 0,16/0,04 + 0,2/2 + 0,04).
            GebaeudeBauteilzeile wand = v.Zeilen.Single(z => z.Bauteil.Bauteilart == DbWerte.BAUTEILART_AUSSENWAND);
            BauteilvorschlagProbe.Nah(1.0 / (0.13 + 0.0009 / 50.0 + 4.0 + 0.1 + 0.04), wand.USchichten, 1e-9);
        }
    }
}
