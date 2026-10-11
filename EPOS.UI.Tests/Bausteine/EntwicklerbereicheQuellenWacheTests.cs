using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// <b>Wache der Entwicklerbereiche, Teil 2: die Fundstellen in den Quelltexten</b> (Konzept Administration und Support
/// 3.3, Entwicklerhandbuch 2.3 und 5). (a) Jeder Schlüssel aus <see cref="Entwicklerbereiche.Alle"/> hat mindestens eine
/// Fundstelle in <c>EPOS.Kern</c>, <c>EPOS.UI</c>, <c>EPOS.UI.Daten</c>, <c>WindowsFormsApplication1</c> oder
/// <c>EPOS.iOS</c> — als <c>Entwicklerbereiche.&lt;SCHLÜSSEL&gt;</c> oder <c>data-bereich="&lt;SCHLÜSSEL&gt;"</c>.
/// (b) Jedes <c>data-stufe="entwickler"</c> in <c>EPOS.UI/**/*.razor</c> trägt im selben Element ein
/// <c>data-bereich</c> mit bekanntem Schlüssel, und die Datei führt ein Kennzeichen (<c>…Freigegeben</c> oder
/// <c>Entwickler</c>) als Parameter oder Gaben-Feld. Gesucht wird vom Repositoriumsstamm aus.
/// </summary>
public class EntwicklerbereicheQuellenWacheTests
{
    private static readonly string[] PROJEKTE = { "EPOS.Kern", "EPOS.UI", "EPOS.UI.Daten", "WindowsFormsApplication1", "EPOS.iOS" };

    private static readonly Regex STUFE = new("data-stufe=\"entwickler\"", RegexOptions.Compiled);
    private static readonly Regex BEREICH = new("data-bereich=\"([A-Z_]+)\"", RegexOptions.Compiled);
    private static readonly Regex KENNZEICHEN = new(
        @"\[Parameter\][^\n]*\b(\w*Freigegeben|Entwickler)\b|\.\w*Freigegeben\b", RegexOptions.Compiled);

    [Fact]
    public void Jeder_Schluessel_hat_eine_Fundstelle()
    {
        List<string> texte = Quelltexte(PROJEKTE, "*.cs", "*.razor").Select(File.ReadAllText).ToList();
        var fehlend = new List<string>();
        foreach (Entwicklerbereich b in Entwicklerbereiche.Alle)
        {
            string code = "Entwicklerbereiche." + b.Schluessel;
            string attribut = "data-bereich=\"" + b.Schluessel + "\"";
            if (!texte.Any(t => Regex.IsMatch(t, Regex.Escape(code) + @"\b") || t.Contains(attribut, StringComparison.Ordinal)))
                fehlend.Add(b.Schluessel);
        }
        Assert.True(fehlend.Count == 0, "Entwicklerbereiche ohne Fundstelle: " + string.Join(", ", fehlend));
    }

    [Fact]
    public void Jede_Entwicklerstufe_im_Markup_nennt_ihren_Bereich_und_die_Datei_ihr_Kennzeichen()
    {
        var bekannt = new HashSet<string>(Entwicklerbereiche.Alle.Select(b => b.Schluessel), StringComparer.Ordinal);
        var befunde = new List<string>();
        int stellen = 0;
        foreach (string datei in Quelltexte(new[] { "EPOS.UI" }, "*.razor"))
        {
            string text = File.ReadAllText(datei);
            MatchCollection treffer = STUFE.Matches(text);
            if (treffer.Count == 0) continue;
            string name = Path.GetFileName(datei);
            foreach (Match m in treffer)
            {
                stellen++;
                int anfang = text.LastIndexOf('<', m.Index);
                int ende = text.IndexOf('>', m.Index);
                string element = anfang < 0 || ende < 0 ? "" : text.Substring(anfang, ende - anfang);
                Match bereich = BEREICH.Match(element);
                if (!bereich.Success) befunde.Add(name + ": data-stufe ohne data-bereich");
                else if (!bekannt.Contains(bereich.Groups[1].Value)) befunde.Add(name + ": unbekannter Bereich " + bereich.Groups[1].Value);
            }
            if (!KENNZEICHEN.IsMatch(text)) befunde.Add(name + ": kein Kennzeichen (…Freigegeben oder Entwickler)");
        }
        Assert.True(stellen > 0, "Keine Stelle mit data-stufe=\"entwickler\" gefunden — die Suche greift nicht.");
        Assert.True(befunde.Count == 0, string.Join(Environment.NewLine, befunde));
    }

    /// <summary>Die Gegenprobe der Regeln: ein Element ohne Bereich und eine Datei ohne Kennzeichen fallen auf.</summary>
    [Fact]
    public void Gegenprobe_ohne_Bereich_und_ohne_Kennzeichen()
    {
        const string text = "<button data-stufe=\"entwickler\" class=\"x\">A</button>\n[Parameter] public bool Aktiv { get; set; }";
        Match m = STUFE.Match(text);
        string element = text.Substring(text.LastIndexOf('<', m.Index), text.IndexOf('>', m.Index) - text.LastIndexOf('<', m.Index));
        Assert.False(BEREICH.IsMatch(element));
        Assert.False(KENNZEICHEN.IsMatch(text));
        Assert.True(KENNZEICHEN.IsMatch("[Parameter] public bool ZusageFreigegeben { get; set; }"));
        Assert.True(KENNZEICHEN.IsMatch("@if (Weg.ProjektdateiFreigegeben)"));
    }

    private static IEnumerable<string> Quelltexte(IEnumerable<string> projekte, params string[] muster)
    {
        string wurzel = Wurzel();
        foreach (string projekt in projekte)
        {
            string ordner = Path.Combine(wurzel, projekt);
            if (!Directory.Exists(ordner)) continue;
            foreach (string m in muster)
                foreach (string datei in Directory.EnumerateFiles(ordner, m, SearchOption.AllDirectories))
                {
                    string rel = Path.GetRelativePath(ordner, datei).Replace('\\', '/');
                    if (rel.StartsWith("bin/", StringComparison.Ordinal) || rel.StartsWith("obj/", StringComparison.Ordinal) ||
                        rel.Contains("/bin/", StringComparison.Ordinal) || rel.Contains("/obj/", StringComparison.Ordinal))
                        continue;
                    yield return datei;
                }
        }
    }

    /// <summary>Die Wurzel des Arbeitsbaums, vom Testausgabeordner aus gesucht.</summary>
    private static string Wurzel()
    {
        var ordner = new DirectoryInfo(AppContext.BaseDirectory);
        while (ordner is not null &&
               !Directory.Exists(Path.Combine(ordner.FullName, "WindowsFormsApplication1", "Views")))
            ordner = ordner.Parent;
        Assert.True(ordner is not null, "Die Wurzel des Arbeitsbaums ist nicht zu finden.");
        return ordner!.FullName;
    }
}
