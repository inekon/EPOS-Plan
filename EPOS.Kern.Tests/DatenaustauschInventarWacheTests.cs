using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Wache „jedes Inventardatum eingestuft“</b> (E138 F5, Konzept Datenaustausch 2 und 4.4): Die Tafel
    /// <see cref="DatenaustauschInventar"/> führt die 71 Daten des Konzepts (A 10, B 22, C 10, D 8, E 7, F 10, G 4) mit eindeutiger
    /// Kennung, jedes mit einer Einstufung für gbXML; und die Einstufung passt zum Code: Was „beide Richtungen“ oder „nur
    /// geschrieben“ heißt, steht in der Datei des Rundlaufs (<see cref="GbxmlExportRundlaufTests"/>), was „beide Richtungen“
    /// oder „nur gelesen“ heißt, nennt der Leser; „ohne Element“ nennt keines.
    /// </summary>
    public class DatenaustauschInventarWacheTests
    {
        [Fact]
        public void Die_Tafel_fuehrt_alle_71_Daten_des_Konzepts_mit_eindeutiger_Kennung()
        {
            Assert.Equal(71, DatenaustauschInventar.Alle.Count);
            Assert.Equal(DatenaustauschInventar.Alle.Count, DatenaustauschInventar.Alle.Select(d => d.Nr).Distinct(StringComparer.Ordinal).Count());
            var soll = new Dictionary<char, int> { ['A'] = 10, ['B'] = 22, ['C'] = 10, ['D'] = 8, ['E'] = 7, ['F'] = 10, ['G'] = 4 };
            Assert.Equal(soll.OrderBy(x => x.Key), DatenaustauschInventar.JeGruppe.OrderBy(x => x.Key));
            foreach (var g in soll)
                Assert.Equal(Enumerable.Range(1, g.Value).Select(i => g.Key + i.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                             DatenaustauschInventar.Alle.Where(d => d.Nr[0] == g.Key).Select(d => d.Nr));
            Assert.All(DatenaustauschInventar.Alle, d => Assert.True(Enum.IsDefined(typeof(Austauschweg), d.Gbxml), d.Nr));
        }

        [Fact]
        public void Die_Einstufung_fuer_gbXML_passt_zu_Schreiber_und_Leser()
        {
            string xml = Encoding.UTF8.GetString(GbxmlExportProbe.Schreiben(GbxmlExportRundlaufTests.Abbild()));
            string leser = string.Concat(Directory.GetFiles(Path.Combine(Repowurzel(), "EPOS.Kern", "Allgemein", "Import", "Gbxml"), "*.cs")
                                                  .Select(File.ReadAllText));
            foreach (Inventardatum d in DatenaustauschInventar.Alle)
            {
                bool schreibt = d.Gbxml == Austauschweg.BeideRichtungen || d.Gbxml == Austauschweg.NurGeschrieben;
                bool liest = d.Gbxml == Austauschweg.BeideRichtungen || d.Gbxml == Austauschweg.NurGelesen;
                if (d.Gbxml == Austauschweg.OhneElement)
                {
                    Assert.Empty(d.Elemente);
                    continue;
                }
                Assert.NotEmpty(d.Elemente);
                foreach (string e in d.Elemente)
                {
                    if (schreibt)
                        Assert.True(xml.Contains("<" + e, StringComparison.Ordinal) || xml.Contains(" " + e + "=\"", StringComparison.Ordinal),
                                    d.Nr + ": " + e + " steht nicht in der exportierten Datei.");
                    if (liest)
                        Assert.True(leser.Contains("\"" + e + "\"", StringComparison.Ordinal), d.Nr + ": " + e + " liest der Leser nicht.");
                }
            }
            // Die Zählung je Einstufung (Protokoll DA-2) - eine Änderung der Tafel zeigt sich hier.
            var zaehlung = DatenaustauschInventar.Alle.GroupBy(d => d.Gbxml).ToDictionary(g => g.Key, g => g.Count());
            Assert.Equal(42, zaehlung[Austauschweg.BeideRichtungen]);
            Assert.Equal(3, zaehlung[Austauschweg.NurGelesen]);
            Assert.False(zaehlung.ContainsKey(Austauschweg.NurGeschrieben));
            Assert.Equal(26, zaehlung[Austauschweg.OhneElement]);
        }

        private static string Repowurzel()
        {
            for (DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory); d != null; d = d.Parent)
                if (File.Exists(Path.Combine(d.FullName, "WP-Plan.sln"))) return d.FullName;
            throw new InvalidOperationException("Repowurzel nicht gefunden.");
        }
    }
}
