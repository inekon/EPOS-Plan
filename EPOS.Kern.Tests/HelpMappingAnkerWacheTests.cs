using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Der Wächter über die ZIELE von <c>help_mapping.txt</c>: Jede Zuordnung
    /// <c>Praefix.Control = Kurzname#anker</c> muss — wo die Zielseite eine Repo-Quelle
    /// unter <c>Projekte/Wiki/</c> oder <c>EPOS.Kern/Allgemein/Hilfe/Berechnung/</c> hat —
    /// auf ein dort tatsächlich vorhandenes <c>{{Anker|...}}</c> treffen (Konzept
    /// Hilfesystem, Abschnitt „Bedienungsseiten mit Repo-Quelle", Regel 4: „Anker bleiben").
    /// Zielseiten ohne Repo-Quelle (reine Live-Wiki-Seiten wie „Lizenz" oder
    /// „Programmablauf") prüft er nicht mit — die Orchestrierung prüft sie vor jedem
    /// Upload von Hand. Der Block „Feldgenaue Hilfe (H12)" bleibt ebenfalls außen vor: Sein
    /// eigener Kopfkommentar in <c>help_mapping.txt</c> erklärt, dass seine Anker erst mit
    /// einem künftigen Sammelimport gesetzt werden und bis dahin auf Seitenebene auflösen —
    /// kein Fehlverhalten. Vorbild: <see cref="WikiProduktdatenWacheTests"/>.
    /// </summary>
    public sealed class HelpMappingAnkerWacheTests
    {
        private static readonly Regex ZuordnungRegex =
            new(@"^([^#=\s][^=]*?)\s*=\s*(.+?)\s*$", RegexOptions.Compiled);
        private static readonly Regex AnkerRegex =
            new(@"\{\{Anker\|([^}]+)\}\}", RegexOptions.Compiled);

        [Fact]
        public void Jeder_Anker_von_help_mapping_existiert_auf_der_Zielseite()
        {
            string wurzel = Arbeitsbaum();
            string mapping = Path.Combine(wurzel, "WindowsFormsApplication1", "Allgemein", "Hilfe", "help_mapping.txt");
            var projekteWiki = LadeWikiOrdner(Path.Combine(wurzel, "Projekte", "Wiki"), "Programm Dokumentation - ");
            var berechnungWiki = LadeWikiOrdner(Path.Combine(wurzel, "EPOS.Kern", "Allgemein", "Hilfe", "Berechnung"), "");

            var fehler = new List<string>();
            bool imFeldgenauenBlock = false;

            foreach (string roh in File.ReadAllLines(mapping))
            {
                string getrimmt = roh.Trim();
                if (getrimmt.Contains("Feldgenaue Hilfe (H12)")) { imFeldgenauenBlock = true; continue; }
                if (getrimmt.Contains("H13 - Rubrik Berechnung")) { imFeldgenauenBlock = false; continue; }
                if (getrimmt.Length == 0 || getrimmt.StartsWith("#") || imFeldgenauenBlock) continue;

                Match m = ZuordnungRegex.Match(roh.TrimEnd());
                string ziel = m.Success ? m.Groups[2].Value.Trim() : "";
                if (!m.Success || !ziel.Contains('#')) continue;

                string[] teile = ziel.Split('#', 2);
                string seite = teile[0].Trim();
                string anker = teile[1].Trim();
                bool istBerechnung = seite.StartsWith("Berechnung/");
                string kurzname = istBerechnung ? seite.Substring("Berechnung/".Length) : seite;
                var quelle = istBerechnung ? berechnungWiki : projekteWiki;

                if (!quelle.TryGetValue(kurzname, out HashSet<string> ankerMenge)) continue; // keine Repo-Quelle
                if (!ankerMenge.Contains(anker))
                    fehler.Add($"{m.Groups[1].Value.Trim()} = {ziel} (Anker '{anker}' fehlt auf {seite})");
            }

            Assert.True(fehler.Count == 0,
                "help_mapping.txt zeigt auf einen Anker, den die Ziel-Wiki-Quelle nicht " +
                "traegt (Anker muessen bei einer Ueberarbeitung erhalten bleiben):\n" +
                string.Join("\n", fehler));
        }

        /// <summary>
        /// Der Hilfeknopf der Nutzflächen-/Verbrauchsangabe (<c>GebaeudeWohnflaecheDialog</c>,
        /// Schlüssel <c>Form_GebWohnflaeche.btn_Help</c>) springt auf den Abschnitt
        /// „Verbrauch" der Seite Gebäude.
        /// </summary>
        [Fact]
        public void Die_Nutzflaechenangabe_springt_auf_den_Abschnitt_Verbrauch()
        {
            string mapping = Path.Combine(Arbeitsbaum(), "WindowsFormsApplication1", "Allgemein", "Hilfe", "help_mapping.txt");

            var ziele = new List<string>();
            foreach (string roh in File.ReadAllLines(mapping))
            {
                Match m = ZuordnungRegex.Match(roh.TrimEnd());
                if (m.Success && string.Equals(m.Groups[1].Value.Trim(), "Form_GebWohnflaeche.btn_Help", StringComparison.Ordinal))
                    ziele.Add(m.Groups[2].Value.Trim());
            }

            Assert.Equal(new[] { "Gebäude#verbrauch" }, ziele);
        }

        private static Dictionary<string, HashSet<string>> LadeWikiOrdner(string ordner, string praefix)
        {
            var ergebnis = new Dictionary<string, HashSet<string>>();
            if (!Directory.Exists(ordner)) return ergebnis;

            foreach (string datei in Directory.EnumerateFiles(ordner, "*.wiki"))
            {
                string name = Path.GetFileNameWithoutExtension(datei);
                if (name.StartsWith("_")) continue;
                string kurzname = name.StartsWith(praefix, StringComparison.Ordinal) ? name.Substring(praefix.Length) : name;

                var anker = new HashSet<string>(StringComparer.Ordinal);
                foreach (Match treffer in AnkerRegex.Matches(File.ReadAllText(datei)))
                    foreach (string teil in treffer.Groups[1].Value.Split('|'))
                        anker.Add(teil.Trim());

                ergebnis[kurzname] = anker;
            }
            return ergebnis;
        }

        /// <summary>Die Wurzel des Arbeitsbaums — derselbe Weg wie in <c>WikiProduktdatenWacheTests</c>.</summary>
        private static string Arbeitsbaum([CallerFilePath] string eigeneDatei = null)
        {
            if (!string.IsNullOrEmpty(eigeneDatei))
            {
                string ordner = Path.GetDirectoryName(eigeneDatei);
                string wurzel = ordner == null ? null : Path.GetDirectoryName(ordner);
                if (wurzel != null && File.Exists(Path.Combine(wurzel, "WP-Plan.sln"))) return wurzel;
            }

            DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d != null)
            {
                if (File.Exists(Path.Combine(d.FullName, "WP-Plan.sln"))) return d.FullName;
                d = d.Parent;
            }

            Assert.Fail("Die Wurzel des Arbeitsbaums (WP-Plan.sln) ist nicht zu finden.");
            return null;
        }
    }
}
