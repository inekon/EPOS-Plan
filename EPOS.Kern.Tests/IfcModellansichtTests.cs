using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Modellansicht (MVD) beim IFC-Import</b>: der Parser von <c>FILE_DESCRIPTION</c>
    /// (<see cref="IfcModellansicht.AusBeschreibung"/>), die Info-Zeile mit Schema und MVD vorn im Protokoll und der eine
    /// Hinweis zur Exporteinstellung, wenn Raumgrenzen 2. Ebene und Basismengen fehlen — an den Proben unter
    /// <c>Referenzlaeufe/Importproben</c>, im Speicher abgewandelt (Kopf, Grenzen, Mengensätze).
    /// </summary>
    public sealed class IfcModellansichtTests : IDisposable
    {
        private const string P = "IMP_IFC_PROT_";
        private const string HINWEIS = P + "EXPORT_OHNE_GRENZEN_MENGEN";

        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        // ==================================================================
        //  Parser
        // ==================================================================

        [Fact]
        public void Leer_null_und_ohne_Kennwort_ergeben_keine_Ansicht()
        {
            Assert.Empty(IfcModellansicht.AusBeschreibung(null));
            Assert.Empty(IfcModellansicht.AusBeschreibung(new string[0]));
            Assert.Empty(IfcModellansicht.AusBeschreibung(new[] { "", "   ", null }));
            Assert.Empty(IfcModellansicht.AusBeschreibung(new[] { "Option [drawingScale: 1:100]" }));
            Assert.Empty(IfcModellansicht.AusBeschreibung(new[] { "ViewDefinition []" }));
            Assert.Empty(IfcModellansicht.AusBeschreibung(new[] { "ViewDefinition CoordinationView_V2.0" }));
        }

        [Theory]
        [InlineData("ViewDefinition [CoordinationView_V2.0]", "CoordinationView_V2.0")]
        [InlineData("ViewDefinition [ReferenceView_V1.2]", "ReferenceView_V1.2")]
        [InlineData("ViewDefinition [DesignTransferView_V1.0]", "DesignTransferView_V1.0")]
        [InlineData("ViewDefinition [CoordinationView]", "CoordinationView")]
        public void Eine_Ansicht(string eintrag, string erwartet)
            => Assert.Equal(new[] { erwartet }, IfcModellansicht.AusBeschreibung(new[] { eintrag }));

        [Fact]
        public void Zwei_Ansichten_in_einer_Klammer_und_in_zwei_Eintraegen()
        {
            Assert.Equal(new[] { "ReferenceView_V1.2", "QuantityTakeOffAddOnView" },
                IfcModellansicht.AusBeschreibung(new[] { "ViewDefinition [ReferenceView_V1.2, QuantityTakeOffAddOnView]" }));
            Assert.Equal(new[] { "CoordinationView_V2.0", "QuantityTakeOffAddOnView" },
                IfcModellansicht.AusBeschreibung(new[] { "ViewDefinition [CoordinationView_V2.0]", "Option [x]",
                                                         "ViewDefinition [QuantityTakeOffAddOnView]" }));
            Assert.Equal(new[] { "CoordinationView_V2.0", "SpaceBoundary2ndLevelAddOnView" },
                IfcModellansicht.AusBeschreibung(new[] { "ViewDefinition [CoordinationView_V2.0;SpaceBoundary2ndLevelAddOnView]" }));
        }

        [Fact]
        public void Gross_und_Kleinschreibung_und_Leerzeichen_gleich_doppelte_einmal()
        {
            Assert.Equal(new[] { "ReferenceView_V1.2" },
                IfcModellansicht.AusBeschreibung(new[] { "  viewdefinition   [  ReferenceView_V1.2  ]  " }));
            Assert.Equal(new[] { "CoordinationView_V2.0" },
                IfcModellansicht.AusBeschreibung(new[] { "VIEWDEFINITION[CoordinationView_V2.0]", "ViewDefinition [coordinationview_v2.0]" }));
        }

        [Fact]
        public void Kaputter_Eintrag_liefert_den_Rest_oder_nichts()
        {
            // Fehlt die schließende Klammer, gilt der Rest des Eintrags; leere Teile fallen weg.
            Assert.Equal(new[] { "CoordinationView_V2.0" },
                IfcModellansicht.AusBeschreibung(new[] { "ViewDefinition [CoordinationView_V2.0" }));
            Assert.Equal(new[] { "ReferenceView_V1.2" },
                IfcModellansicht.AusBeschreibung(new[] { "ViewDefinition [ , ReferenceView_V1.2,, ]" }));
            Assert.Empty(IfcModellansicht.AusBeschreibung(new[] { "ViewDefinition [" }));
            Assert.Empty(IfcModellansicht.AusBeschreibung(new[] { "ViewDefinition" }));
            Assert.Equal(new[] { "ReferenceView" }, IfcModellansicht.AusBeschreibung(new[] { "ViewDefinition ['ReferenceView']" }));
        }

        [Theory]
        [InlineData("BaseQuantities", true)]
        [InlineData("Qto_WallBaseQuantities", true)]
        [InlineData("qto_spacequantities", true)]
        [InlineData("Pset_WallCommon", false)]
        [InlineData("Mengen", false)]
        [InlineData("", false)]
        public void Basismengensatz_ueber_den_Namen(string name, bool erwartet)
            => Assert.Equal(erwartet, IfcModellansicht.IstBasismengensatz(name));

        // ==================================================================
        //  Protokollzeilen an den Proben
        // ==================================================================

        [Theory]
        [InlineData("ifc4_haus.ifc", "IFC4")]
        [InlineData("ifc2x3_haus.ifc", "IFC2X3")]
        public void Mit_MVD_steht_die_Info_Zeile_vorn_ohne_Hinweis(string probe, string schema)
        {
            var abbild = Lesen(Abwandeln(Text(probe), "('ViewDefinition [ReferenceView_V1.2, QuantityTakeOffAddOnView]')"));
            Assert.Equal(new[] { "ReferenceView_V1.2", "QuantityTakeOffAddOnView" }, abbild.Modellansichten);
            PruefMeldung m = abbild.Meldungen[0];
            Assert.Equal(P + "DATEI_MVD", m.Schluessel);
            Assert.Equal(PruefStufe.Info, m.Stufe);
            Assert.Equal(new[] { schema, "ReferenceView_V1.2, QuantityTakeOffAddOnView" }, m.Werte);
            Assert.Equal("Datei: " + schema + ", Modellansicht (MVD): ReferenceView_V1.2, QuantityTakeOffAddOnView",
                Text(m));
            Assert.DoesNotContain(abbild.Meldungen, x => x.Schluessel.StartsWith(HINWEIS, StringComparison.Ordinal));
        }

        [Fact]
        public void Ohne_MVD_nennt_die_Zeile_keine_Modellansicht()
        {
            var abbild = Lesen(Abwandeln(Text("ifc4_haus.ifc"), "('')"));
            Assert.Empty(abbild.Modellansichten);
            PruefMeldung m = abbild.Meldungen[0];
            Assert.Equal(P + "DATEI_OHNE_MVD", m.Schluessel);
            Assert.Equal("Datei: IFC4, keine Modellansicht (MVD) angegeben", Text(m));
            Assert.Single(abbild.Meldungen, x => x.Schluessel.StartsWith(P + "DATEI_", StringComparison.Ordinal));
        }

        [Fact]
        public void Nur_Grenzen_oder_nur_Mengen_genuegen_ohne_Hinweis()
        {
            // Nur Raumgrenzen 2. Ebene (Mengensätze umbenannt) …
            var nurGrenzen = Lesen(Abwandeln(Text("ifc4_haus.ifc"), "('')", mengenWeg: true));
            Assert.DoesNotContain(nurGrenzen.Meldungen, x => x.Schluessel.StartsWith(HINWEIS, StringComparison.Ordinal));
            // … oder nur Basismengen (Grenzen auf die 1. Ebene gesetzt).
            var nurMengen = Lesen(Abwandeln(Text("ifc4_haus.ifc"), "('')", grenzenWeg: true));
            Assert.DoesNotContain(nurMengen.Meldungen, x => x.Schluessel.StartsWith(HINWEIS, StringComparison.Ordinal));
        }

        [Fact]
        public void Ohne_Grenzen_und_Mengen_steht_genau_ein_Hinweis_IFC4()
        {
            var abbild = Lesen(Abwandeln(Text("ifc4_haus.ifc"), "('ViewDefinition [CoordinationView_V2.0]')", grenzenWeg: true, mengenWeg: true));
            Assert.Equal(P + "DATEI_MVD", abbild.Meldungen[0].Schluessel);
            PruefMeldung h = Assert.Single(abbild.Meldungen, x => x.Schluessel.StartsWith(HINWEIS, StringComparison.Ordinal));
            Assert.Equal(HINWEIS, h.Schluessel);
            Assert.Equal(PruefStufe.Info, h.Stufe);
            Assert.StartsWith("Die Datei enthält keine Raumgrenzen 2. Ebene und keine Basismengen", Text(h));
            Assert.DoesNotContain("IFC2X3", Text(h));
        }

        [Fact]
        public void Ohne_Grenzen_und_Mengen_IFC2X3_mit_Zusatz()
        {
            var abbild = Lesen(Abwandeln(Text("ifc2x3_haus.ifc"), "('')", grenzenWeg: true, mengenWeg: true));
            PruefMeldung h = Assert.Single(abbild.Meldungen, x => x.Schluessel.StartsWith(HINWEIS, StringComparison.Ordinal));
            // Die Hausprobe trägt keine Baustoffe: keine Sammelmeldung, also nennt der Hinweis die Stoffwerte selbst.
            Assert.DoesNotContain(abbild.Meldungen, x => x.Schluessel == P + "STOFFWERTE_NICHT_GELESEN");
            Assert.Equal(HINWEIS + "_IFC2X3", h.Schluessel);
            Assert.Contains("IFC4", Text(h));
            Assert.Contains("Stoffwerte", Text(h));
        }

        [Fact]
        public void IFC2X3_mit_Stoffwerten_nennt_nur_die_Rueckgabe()
        {
            var abbild = Lesen(Abwandeln(Text("ifc2x3_schichten.ifc"), "('')", grenzenWeg: true, mengenWeg: true));
            Assert.Contains(abbild.Meldungen, x => x.Schluessel == P + "STOFFWERTE_NICHT_GELESEN");
            PruefMeldung h = Assert.Single(abbild.Meldungen, x => x.Schluessel.StartsWith(HINWEIS, StringComparison.Ordinal));
            Assert.Equal(HINWEIS + "_IFC2X3_RUECKGABE", h.Schluessel);
            Assert.DoesNotContain("Stoffwerte", Text(h));
        }

        [Fact]
        public void Alle_neuen_Schluessel_stehen_in_beiden_Sprachen()
        {
            string[] schluessel =
            {
                P + "DATEI_MVD", P + "DATEI_OHNE_MVD", HINWEIS, HINWEIS + "_IFC2X3", HINWEIS + "_IFC2X3_RUECKGABE",
                "GIMP_DLG_IFC_EXPORTHINWEIS", "GIMP_DLG_KOPF_MVD", "GIMP_WERT_KEINE_MVD",
            };
            foreach (string k in schluessel)
            {
                Assert.False(string.IsNullOrWhiteSpace(R.ResourceManager.GetString(k, new System.Globalization.CultureInfo("de-DE"))), k);
                Assert.False(string.IsNullOrWhiteSpace(R.ResourceManager.GetString(k, new System.Globalization.CultureInfo("en-US"))), k);
            }
            Assert.Equal("GIMP_DLG_IFC_EXPORTHINWEIS", new IfcImportProfil().ExporthinweisSchluessel);
            Assert.Equal("", new GbxmlImportProfil().ExporthinweisSchluessel);
        }

        // ==================================================================
        //  Zugang
        // ==================================================================

        private static string Text(string probe)
            => File.ReadAllText(Path.Combine(IfcProbenTests.Ordner(), probe), Encoding.UTF8);

        private static string Text(PruefMeldung m)
            => string.Format(R.ResourceManager.GetString(m.Schluessel), m.Werte);

        /// <summary>
        /// Wandelt eine Probe ab: <c>FILE_DESCRIPTION</c> mit der gegebenen Liste; Raumgrenzen 2. Ebene auf die 1. Ebene
        /// (Name und Beschreibung), Mengensätze auf einen Namen, den der Import nicht liest.
        /// </summary>
        private static string Abwandeln(string text, string beschreibung, bool grenzenWeg = false, bool mengenWeg = false)
        {
            string neu = Regex.Replace(text, @"FILE_DESCRIPTION\s*\(\(.*?\)\s*,", "FILE_DESCRIPTION(" + beschreibung + ",");
            Assert.NotEqual(text, neu);
            if (grenzenWeg)
            {
                neu = Regex.Replace(neu, @"(IFCRELSPACEBOUNDARY\('[^']*',[^,]*,'[^']*',)'2[ab]'", "$1$");
                neu = neu.Replace("'2ndLevel'", "'1stLevel'");
                Assert.DoesNotContain("'2ndLevel'", neu);
            }
            if (mengenWeg)
                neu = Regex.Replace(neu, @"(IFCELEMENTQUANTITY\('[^']*',[^,]*,)'[^']*'", "$1'Eigene Mengen'");
            return neu;
        }

        private static IfcGebaeudeAbbild Lesen(string text)
        {
            using var s = new MemoryStream(Encoding.UTF8.GetBytes(text));
            var abbild = (IfcGebaeudeAbbild)new IfcLeser().Lesen(s, new IfcImportProfil(), null, default);
            Assert.DoesNotContain(abbild.Meldungen, m => m.Stufe == PruefStufe.Fehler);
            return abbild;
        }
    }
}
