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
    /// eines Schichtsatzes über 1 m, gilt der ganze Satz als Millimeter; Schichten unter 1 mm (Folien) werden
    /// übergangen — beides mit Hinweis je Satz. Probe: <c>ifc4_schichtdicken_mm.ifc</c>.
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
            Assert.Equal(2, wand.Aufbau.Schichten.Count);
            Assert.Equal(new[] { "Dämmung", "Beton" }, wand.Aufbau.Schichten.Select(s => s.Name));
            BauteilvorschlagProbe.Nah(0.16, wand.Aufbau.Schichten[0].DickeM);
            BauteilvorschlagProbe.Nah(0.20, wand.Aufbau.Schichten[1].DickeM);
            BauteilvorschlagProbe.Nah(0.36, wand.DickeM);

            PruefMeldung mm = Assert.Single(a.Abbild.Meldungen, m => m.Schluessel == "IMP_IFC_PROT_SCHICHTDICKE_MM");
            Assert.Equal(PruefStufe.Warnung, mm.Stufe);
            Assert.Equal(wand.Aufbau.Kennung, mm.Werte[0]);
            Assert.Equal("Außenwand Millimeter", mm.Werte[1]);
            Assert.Equal("200", mm.Werte[2]);
            PruefMeldung duenn = Assert.Single(a.Abbild.Meldungen, m => m.Schluessel == "IMP_IFC_PROT_SCHICHT_DUENN");
            Assert.Equal(PruefStufe.Info, duenn.Stufe);
            Assert.Equal(wand.Aufbau.Kennung, duenn.Werte[0]);
            Assert.Equal("1", duenn.Werte[2]);
            Assert.Equal("Folie (0.2 mm)", duenn.Werte[3]);
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
            Assert.DoesNotContain(a.Abbild.Meldungen, m => (m.Schluessel == "IMP_IFC_PROT_SCHICHTDICKE_MM"
                                                            || m.Schluessel == "IMP_IFC_PROT_SCHICHT_DUENN")
                                                           && m.Werte[0] == dach.Aufbau.Kennung);
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
                Assert.Equal(2, BauteilvorschlagProbe.Aufbau(v, z).Schichten.Count);
            }
            // U der Wand aus 0,16 m Dämmung und 0,20 m Beton: 1/(0,13 + 0,16/0,04 + 0,2/2 + 0,04).
            GebaeudeBauteilzeile wand = v.Zeilen.Single(z => z.Bauteil.Bauteilart == DbWerte.BAUTEILART_AUSSENWAND);
            BauteilvorschlagProbe.Nah(1.0 / (0.13 + 4.0 + 0.1 + 0.04), wand.USchichten, 1e-9);
        }
    }
}
