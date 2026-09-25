using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging.Abstractions;
using WindowsFormsApplication1;
using Xbim.Common;
using Xbim.Common.Step21;
using Xbim.Ifc4.Interfaces;
using Xbim.IO.Memory;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Rückbeziehungen als Index</b> (<see cref="IfcRueckbezuege"/>): Der IFC-Leser fragt
    /// <c>IsDefinedBy</c>, <c>IsTypedBy</c>, <c>HasAssociations</c>, <c>IsDecomposedBy</c>,
    /// <c>ContainsElements</c>, <c>HasOpenings</c>, <c>HasFillings</c> und <c>HasProperties</c> aus einem
    /// Index, der jede Beziehungsart einmal je Modell durchläuft — statt je Frage das ganze Modell zu
    /// durchsuchen (O(n²)). Geprüft wird, dass der Index für jedes Objekt der Proben dieselben Beziehungen
    /// in derselben Reihenfolge liefert wie die Bibliothek, warum der Zwischenspeicher der Bibliothek kein
    /// Ersatz ist, und dass der Leser keine Rückbeziehung mehr an der Bibliothek fragt. Die grobe
    /// Linearität in der Zeit hält <c>ImportmessungTests</c>.
    /// </summary>
    public sealed class IfcRueckbezuegeTests
    {
        public static IEnumerable<object[]> Proben() => new[]
        {
            // Datei, trägt Schichten (HasAssociations, HasProperties), trägt Öffnungen (HasOpenings, HasFillings)
            new object[] { "ifc4_haus.ifc", false, true },
            new object[] { "ifc2x3_haus.ifc", false, true },
            new object[] { "ifc4_schichten.ifc", true, true },
            new object[] { "ifc2x3_schichten.ifc", true, true },
            new object[] { "ifc4_rueckfaelle.ifc", false, false },
            new object[] { "ifc4_mapconversion.ifc", false, false },
            new object[] { "ifc4_ohne_mengen.ifc", false, false },
            new object[] { "synthetisch", false, true },
        };

        [Theory]
        [MemberData(nameof(Proben))]
        public void Der_Index_antwortet_wie_die_Rueckbeziehungen_der_Bibliothek(string probe, bool schichten, bool oeffnungen)
        {
            byte[] daten = probe == "synthetisch" ? ImportmessungProben.Ifc(120) : File.ReadAllBytes(Path.Combine(IfcProbenTests.Ordner(), probe));
            using (MemoryModel m = Laden(daten))
            {
                var r = new IfcRueckbezuege(m);
                var paare = new Dictionary<string, int>(StringComparer.Ordinal);

                foreach (IIfcObjectDefinition o in m.Instances.OfType<IIfcObjectDefinition>())
                {
                    Gleich(paare, "HasAssociations", o, o.HasAssociations, r.Zuordnungen(o));
                    Gleich(paare, "IsDecomposedBy", o, o.IsDecomposedBy, r.ZerlegtDurch(o));
                }
                foreach (IIfcObject o in m.Instances.OfType<IIfcObject>())
                {
                    Gleich(paare, "IsDefinedBy", o, o.IsDefinedBy, r.DefiniertDurch(o));
                    Gleich(paare, "IsTypedBy", o, o.IsTypedBy, r.TypisiertDurch(o));
                }
                foreach (IIfcContext k in m.Instances.OfType<IIfcContext>())
                    Gleich(paare, "IsDefinedBy(Kontext)", k, k.IsDefinedBy, r.DefiniertDurch(k));
                foreach (IIfcSpatialElement s in m.Instances.OfType<IIfcSpatialElement>())
                    Gleich(paare, "ContainsElements", s, s.ContainsElements, r.Enthaelt(s));
                foreach (IIfcElement e in m.Instances.OfType<IIfcElement>())
                    Gleich(paare, "HasOpenings", e, e.HasOpenings, r.Oeffnungen(e));
                foreach (IIfcOpeningElement o in m.Instances.OfType<IIfcOpeningElement>())
                    Gleich(paare, "HasFillings", o, o.HasFillings, r.Fuellungen(o));
                foreach (IIfcMaterialDefinition d in m.Instances.OfType<IIfcMaterialDefinition>())
                    Gleich(paare, "HasProperties", d, d.HasProperties, r.Stoffsaetze(d));

                // Nicht leer verglichen: Jede Probe trägt, was der Leser fragt.
                foreach (string was in new[] { "IsDefinedBy", "IsDecomposedBy", "ContainsElements" })
                    Assert.True(paare.GetValueOrDefault(was) > 0, probe + ": keine " + was);
                if (schichten)
                    foreach (string was in new[] { "HasAssociations", "HasProperties" })
                        Assert.True(paare.GetValueOrDefault(was) > 0, probe + ": keine " + was);
                if (oeffnungen)
                    foreach (string was in new[] { "HasOpenings", "HasFillings" })
                        Assert.True(paare.GetValueOrDefault(was) > 0, probe + ": keine " + was);
            }
        }

        [Fact]
        public void Befund_der_Zwischenspeicher_der_Bibliothek_verliert_die_Stoffsaetze_von_IFC2X3()
        {
            // Hält fest, warum der Leser nicht BeginInverseCaching nimmt (xBIM 6.1.605): IfcMaterialProperties
            // von IFC2X3 führen keine indizierten Verweise (IfcMaterial.HasProperties gibt es erst ab IFC4),
            // und der Zwischenspeicher liefert dann eine leere Menge. Ohne Stoffsätze entfiele die Meldung
            // IMP_IFC_PROT_STOFFWERTE_NICHT_GELESEN (IfcImportWelle2Tests.IFC2X3_Stoffwerte_werden_benannt_nicht_gelesen).
            using (MemoryModel m = Laden(File.ReadAllBytes(Path.Combine(IfcProbenTests.Ordner(), "ifc2x3_schichten.ifc"))))
            {
                List<IIfcMaterial> stoffe = m.Instances.OfType<IIfcMaterial>().ToList();
                int ohne = stoffe.Sum(s => s.HasProperties.Count());
                var r = new IfcRueckbezuege(m);
                int index = stoffe.Sum(s => r.Stoffsaetze(s).Count);
                int mit;
                using (m.BeginInverseCaching())
                    mit = stoffe.Sum(s => s.HasProperties.Count());

                Assert.Equal(9, ohne);
                Assert.Equal(ohne, index);
                Assert.Equal(0, mit);
                Assert.All(m.Instances.OfType<IIfcMaterialProperties>(), p => Assert.False(p is IContainsIndexedReferences));
            }
        }

        [Fact]
        public void Ein_Ziel_ohne_Beziehung_und_kein_Ziel_ergeben_leere_Listen()
        {
            using (MemoryModel m = Laden(ImportmessungProben.Ifc(3)))
            {
                IIfcSpace raum = m.Instances.OfType<IIfcSpace>().First();
                var r = new IfcRueckbezuege(m);
                Assert.NotEmpty(r.DefiniertDurch(raum));
                Assert.Empty(r.ZerlegtDurch(raum));
                Assert.Empty(r.Oeffnungen(null));
            }
        }

        [Fact]
        public void Quelltextwache_der_Leser_fragt_keine_Rueckbeziehung_an_der_Bibliothek()
        {
            // Jede dieser Eigenschaften durchsucht ohne Zwischenspeicher das ganze Modell; der Leser nimmt
            // IfcRueckbezuege. HasProperties ist zugleich ein Vorwärtsattribut von IfcPropertySet — erlaubt
            // bleibt genau diese Stelle. HasCoordinateOperation wird einmal je Datei gefragt und ist erlaubt.
            string ordner = Path.Combine(Wurzel(), "EPOS.Kern", "Allgemein", "Import", "Ifc");
            const string muster = @"\.(IsDefinedBy|IsTypedBy|HasAssociations|IsDecomposedBy|Decomposes|ContainsElements|ContainedInStructure"
                                  + @"|HasOpenings|HasFillings|FillsVoids|VoidsElements|BoundedBy|ProvidesBoundaries|HasProperties|AssociatedTo"
                                  + @"|IsNestedBy|Nests|ReferencedBy|Types|DefinesOccurrence|PlacesObject|ReferencedByPlacements)\b";
            var funde = new List<string>();
            foreach (string datei in Directory.GetFiles(ordner, "*.cs").OrderBy(d => d, StringComparer.Ordinal))
            {
                if (Path.GetFileName(datei) == "IfcRueckbezuege.cs") continue;
                string[] zeilen = Code(datei).Split('\n');
                for (int i = 0; i < zeilen.Length; i++)
                    foreach (Match treffer in Regex.Matches(zeilen[i], muster))
                        if (!(treffer.Value == ".HasProperties" && zeilen[i].Contains("ps.HasProperties")))
                            funde.Add(Path.GetFileName(datei) + ":" + (i + 1) + " " + zeilen[i].Trim());
            }
            Assert.True(funde.Count == 0, "Rückbeziehung an der Bibliothek statt aus IfcRueckbezuege:\n" + string.Join("\n", funde));
        }

        // ==================================================================
        //  Hilfen
        // ==================================================================

        private static void Gleich<T>(Dictionary<string, int> paare, string was, IPersistEntity e, IEnumerable<T> bibliothek, IReadOnlyList<T> index)
            where T : IPersistEntity
        {
            int[] erwartet = bibliothek.Select(x => x.EntityLabel).ToArray();
            int[] ist = index.Select(x => x.EntityLabel).ToArray();
            Assert.True(erwartet.SequenceEqual(ist),
                was + " von #" + e.EntityLabel + ": Bibliothek [" + string.Join(",", erwartet) + "], Index [" + string.Join(",", ist) + "]");
            paare[was] = paare.GetValueOrDefault(was) + erwartet.Length;
        }

        private static MemoryModel Laden(byte[] daten)
        {
            XbimSchemaVersion schema = MemoryModel.GetStepFileXbimSchemaVersion(new MemoryStream(daten, false));
            var m = new MemoryModel(MemoryModel.GetFactory(schema), NullLoggerFactory.Instance, 0);
            using (var strom = new MemoryStream(daten, false))
                m.LoadStep21(strom, daten.LongLength, null, null);
            return m;
        }

        private static string Code(string datei)
            => Regex.Replace(File.ReadAllText(datei), @"//[^\r\n]*|/\*.*?\*/", "", RegexOptions.Singleline);

        private static string Wurzel([CallerFilePath] string eigeneDatei = null)
        {
            string ordner = Path.GetDirectoryName(eigeneDatei);
            while (ordner != null && !File.Exists(Path.Combine(ordner, "WP-Plan.Kern.slnf")))
                ordner = Path.GetDirectoryName(ordner);
            Assert.True(ordner != null, "Die Wurzel des Arbeitsbaums ist nicht zu finden.");
            return ordner;
        }
    }
}
