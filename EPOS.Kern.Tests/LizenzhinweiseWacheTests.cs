using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Wächter über die Lizenzhinweisseite</b> (Anwenderentscheid E27, Frage U10;
    /// Umsetzungskonzept Gebäudesimulation 3.6, Datenaustauschkonzept 8.1).
    ///
    /// <para><b>Die Regel:</b> Jede <c>PackageVersion</c> aus <c>Directory.Packages.props</c>, die ein
    /// AUSGELIEFERTES Projekt direkt referenziert — Kern, Oberfläche, Hüllen, KI-Kern, Speicher-Engine,
    /// Speicherplanung, Windows-Schale —, steht mit ihrem Namen in
    /// <c>Setup/Vorlage/Lizenzhinweise.txt</c>. Der Pflegeweg ist damit eine Zeile je Paket: Wer ein
    /// Paket aufnimmt, wird hier daran erinnert, seinen Lizenzvermerk einzutragen.</para>
    ///
    /// <para><b>Für xBIM gilt mehr</b> (CDDL-1.0, E3): Der Eintrag nennt die Lizenz „CDDL-1.0", die
    /// Fassung aus <c>Directory.Packages.props</c> und den dauerhaften Quelltextverweis nach CDDL § 3.1
    /// — ohne ihn ist der IFC-Import nicht auslieferbar.</para>
    ///
    /// <para>Test- und Werkzeugprojekte und die iOS-Hülle zählen nicht; reine Build- oder
    /// Analysepakete stehen benannt in <see cref="NurBauUndAnalyse"/>.</para>
    /// </summary>
    public sealed class LizenzhinweiseWacheTests
    {
        /// <summary>Die ausgelieferten Projekte, repo-relativ.</summary>
        private static readonly string[] AusgelieferteProjekte =
        {
            "EPOS.Kern/EPOS.Kern.csproj",
            "EPOS.UI/EPOS.UI.csproj",
            "EPOS.UI.Daten/EPOS.UI.Daten.csproj",
            "KiKern/KiKern.csproj",
            "SpeicherEngine/SpeicherEngine.csproj",
            "SpeicherPlanung/SpeicherPlanung.csproj",
            "WindowsFormsApplication1/WindowsFormsApplication1.csproj",
        };

        /// <summary>
        /// Pakete, die ein ausgeliefertes Projekt referenziert, die aber NICHTS in die Auslieferung
        /// legen — reine Build- oder Analysepakete (Analyzer, Quelltextgeneratoren). Jede Zeile trägt
        /// ihre Begründung. Derzeit leer: Kein ausgeliefertes Projekt referenziert ein solches Paket
        /// direkt (die Analyzer der Razor-Komponenten kommen transitiv und legen keine Datei ab).
        /// </summary>
        private static readonly Dictionary<string, string> NurBauUndAnalyse =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
            };

        private const string Seite = "Setup/Vorlage/Lizenzhinweise.txt";

        [Fact]
        public void Jedes_ausgelieferte_Paket_steht_auf_der_Lizenzhinweisseite()
        {
            Dictionary<string, string> fassungen = Paketfassungen();
            string text = File.ReadAllText(Pfad(Seite));

            var referenziert = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string projekt in AusgelieferteProjekte)
                foreach (string paket in Paketverweise(Pfad(projekt)))
                    referenziert.Add(paket);

            Assert.True(referenziert.Count >= 20, "Nur " + referenziert.Count + " Paketverweise gefunden — liest der Wächter die Projekte?");

            var funde = new List<string>();
            foreach (string paket in referenziert)
            {
                if (NurBauUndAnalyse.ContainsKey(paket)) continue;
                if (!fassungen.ContainsKey(paket))
                    funde.Add(paket + ": kein Eintrag in Directory.Packages.props");
                else if (!Genannt(text, paket))
                    funde.Add(paket + " " + fassungen[paket] + ": fehlt in " + Seite);
            }
            Assert.True(funde.Count == 0,
                "Die Lizenzhinweisseite ist nicht vollständig (E27, U10) — je ausgelieferter Paketfassung " +
                "Name, Fassung, Lizenz, Copyright-Vermerk und Quelltextverweis aus der .nuspec eintragen:\n" +
                string.Join("\n", funde));
        }

        [Fact]
        public void Der_xBIM_Eintrag_nennt_CDDL_Fassung_und_Quelltextverweis()
        {
            Dictionary<string, string> fassungen = Paketfassungen();
            Assert.True(fassungen.TryGetValue("Xbim.IO.MemoryModel", out string fassung),
                "Xbim.IO.MemoryModel fehlt in Directory.Packages.props.");
            string text = File.ReadAllText(Pfad(Seite));

            string block = Abschnitt(text, "Xbim.IO.MemoryModel");
            Assert.Contains("CDDL-1.0", block);
            Assert.Contains("Xbim.IO.MemoryModel " + fassung, block);
            Assert.Contains("https://github.com/xBimTeam/XbimEssentials", block);
            Assert.Contains("3.1", block);          // der Verweis nach CDDL § 3.1
            Assert.Contains("unverändert", block);  // E3: nie geforkt, nie gepatcht
            foreach (string schema in new[] { "Xbim.Common", "Xbim.Ifc2x3", "Xbim.Ifc4", "Xbim.Ifc4x3" })
                Assert.True(Genannt(block, schema), schema + " fehlt im xBIM-Eintrag.");
        }

        [Fact]
        public void Das_Setup_legt_die_Lizenzhinweise_nach_app()
        {
            string iss = File.ReadAllText(Pfad("Setup/EPOS-Plan.iss"));
            Assert.Contains("Vorlage\\Lizenzhinweise.txt", iss);
            Assert.Matches(new Regex(@"^Source:\s*""\{#Lizenzhinweise\}"";\s*DestDir:\s*""\{app\}""", RegexOptions.Multiline), iss);
        }

        [Fact]
        public void Im_Kern_steht_kein_Esent_kein_Geometriekern_und_kein_Metapaket()
        {
            Dictionary<string, string> fassungen = Paketfassungen();
            foreach (string verboten in fassungen.Keys.Where(k => k.StartsWith("Xbim.", StringComparison.OrdinalIgnoreCase)))
                Assert.True(string.Equals(verboten, "Xbim.IO.MemoryModel", StringComparison.OrdinalIgnoreCase),
                    verboten + " in Directory.Packages.props — der Kern nimmt allein Xbim.IO.MemoryModel " +
                    "(ADR-003: kein Xbim.Essentials, kein Xbim.Ifc, kein Xbim.IO.Esent, kein Xbim.Geometry).");
            Assert.Equal("6.1.605", fassungen["Xbim.IO.MemoryModel"]);
            foreach (string projekt in AusgelieferteProjekte)
                foreach (string paket in Paketverweise(Pfad(projekt)).Where(p => p.StartsWith("Xbim.", StringComparison.OrdinalIgnoreCase)))
                    Assert.True(projekt == "EPOS.Kern/EPOS.Kern.csproj" && paket == "Xbim.IO.MemoryModel",
                        projekt + " referenziert " + paket + " — das Paket hängt allein am Kern (A2).");
        }

        // ==================================================================
        //  Hilfen
        // ==================================================================

        /// <summary>Ist der Paketname als ganzes Wort genannt (nicht nur als Präfix eines längeren Namens)?</summary>
        private static bool Genannt(string text, string paket)
            => Regex.IsMatch(text, @"(?<![\w.])" + Regex.Escape(paket) + @"(?![\w.])", RegexOptions.IgnoreCase);

        /// <summary>Der Abschnitt der Seite, in dem der Name steht — bis zur nächsten Trennlinie.</summary>
        private static string Abschnitt(string text, string name)
        {
            int i = text.IndexOf(name, StringComparison.Ordinal);
            Assert.True(i >= 0, name + " steht nicht auf der Seite.");
            int ende = text.IndexOf("\n----", i, StringComparison.Ordinal);
            return ende < 0 ? text.Substring(i) : text.Substring(i, ende - i);
        }

        private static Dictionary<string, string> Paketfassungen()
        {
            XDocument d = XDocument.Load(Pfad("Directory.Packages.props"));
            return d.Descendants().Where(e => e.Name.LocalName == "PackageVersion")
                    .Select(e => (Name: (string)e.Attribute("Include"), Fassung: (string)e.Attribute("Version")))
                    .Where(p => p.Name != null)
                    .GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First().Fassung, StringComparer.OrdinalIgnoreCase);
        }

        private static IEnumerable<string> Paketverweise(string projekt)
        {
            XDocument d = XDocument.Load(projekt);
            foreach (XElement e in d.Descendants().Where(e => e.Name.LocalName == "PackageReference"))
            {
                string name = (string)e.Attribute("Include");
                if (!string.IsNullOrWhiteSpace(name)) yield return name.Trim();
            }
        }

        private static string Pfad(string relativ) => Path.Combine(Wurzel(), relativ.Replace('/', Path.DirectorySeparatorChar));

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
