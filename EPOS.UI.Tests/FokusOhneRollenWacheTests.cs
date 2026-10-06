using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace EPOS.UI.Tests;

/// <summary>
/// Die Quelltextwache zur Hausregel <b>„Ein Fokus rollt nicht"</b> (Anwenderbefund vom
/// 06.10.2026: Unter „Berichte &amp; Kosten" sprang die Ansicht beim Reiterwechsel nach unten,
/// Auftrag „Prüfe auch bei anderen Dialogen diesen Sprung").
///
/// <para><b>Der Befund:</b> Fast jeder Dialog, jede Überlagerung und jede Seite setzt beim ersten
/// Zeichnen den Fokus auf seine Wurzel, damit Esc und der Tabulator dort beginnen. Ein
/// <c>FocusAsync()</c> ohne <c>preventScroll</c> rollt den Browser so, dass das Element sichtbar
/// wird: Ist der Dialog höher als sein Rollbehälter oder steht er nicht oben darin, zentriert
/// Chromium ihn und schiebt Dialogkopf, Reiter oder Schlussleiste aus dem Bild — gemessen mit
/// <c>Proben/Rasterprobe/fokusprobe.mjs</c> (Wärmepumpen im eigenen Fenster um mehr als 1 000 px,
/// die Überlagerung „Simulation..." des Gebäudedialogs um 70 px samt verdecktem Kopf).</para>
///
/// <para><b>Die Regel:</b> Jeder Aufruf <c>FocusAsync(</c> unter <c>EPOS.UI</c> trägt
/// <c>preventScroll: true</c> — oder einen Vermerk <c>Rollen gewollt: &lt;Grund&gt;</c> in
/// derselben oder der Zeile davor (gezielter Sprung zum Fehlerfeld oder zum gewählten Eintrag,
/// Tastaturnavigation). Eine neue Stelle ohne beides ist rot.</para>
///
/// <para><b>Die Grenze der Wache:</b> Sie liest Quelltext, keinen gezeichneten Baum; ob ein Dialog
/// tatsächlich springt, misst allein die Fokusprobe im echten Browser.</para>
/// </summary>
public sealed class FokusOhneRollenWacheTests
{
    private const string VERMERK = "Rollen gewollt:";

    private static readonly Regex VERMERK_MIT_GRUND =
        new(@"Rollen gewollt:\s*\S", RegexOptions.CultureInvariant);

    /// <summary>
    /// Die ungeschützten Stellen eines Quelltexts als „Zeile: Text". Leer = jede Stelle trägt
    /// <c>preventScroll: true</c> oder den Vermerk mit Grund.
    /// </summary>
    internal static List<string> Ungeschuetzt(string quelltext)
    {
        string[] zeilen = quelltext.Replace("\r\n", "\n").Split('\n');
        var befund = new List<string>();
        for (int i = 0; i < zeilen.Length; i++)
        {
            string zeile = zeilen[i];
            if (!zeile.Contains("FocusAsync(", StringComparison.Ordinal)) continue;
            if (zeile.Contains("preventScroll: true", StringComparison.Ordinal)) continue;
            if (VERMERK_MIT_GRUND.IsMatch(zeile)) continue;
            if (i > 0 && VERMERK_MIT_GRUND.IsMatch(zeilen[i - 1])) continue;
            befund.Add($"{i + 1}: {zeile.Trim()}");
        }
        return befund;
    }

    [Fact]
    public void Jeder_Fokus_unter_EPOS_UI_rollt_nicht_oder_sagt_warum()
    {
        string wurzel = Wurzel();
        string ui = Path.Combine(wurzel, "EPOS.UI");
        var befund = new List<string>();
        int stellen = 0;
        foreach (string voll in Dateien(ui))
        {
            string text = File.ReadAllText(voll);
            stellen += Regex.Matches(text, @"FocusAsync\(").Count;
            string rel = Path.GetRelativePath(wurzel, voll).Replace('\\', '/');
            befund.AddRange(Ungeschuetzt(text).Select(b => rel + ":" + b));
        }

        // Die Wache misst etwas: Der Bestand trägt rund hundert Fokusstellen.
        Assert.True(stellen >= 50, $"nur {stellen} Stellen FocusAsync( gefunden — liest die Wache den falschen Ordner?");
        Assert.True(befund.Count == 0,
            "FocusAsync ohne preventScroll: true und ohne Vermerk „" + VERMERK + " <Grund>\" — ein Erstfokus " +
            "rollt den Dialog aus dem Bild (Proben/Rasterprobe/fokusprobe.mjs):\n" + string.Join("\n", befund));
    }

    [Fact]
    public void Gegenprobe_ohne_Schutz_ist_die_Wache_rot()
    {
        // Eine echte Stelle des Bestands: die Wurzel der Überlagerung. Ohne den Schutz meldet die
        // Wache sie, mit ihm nicht.
        string text = File.ReadAllText(Path.Combine(Wurzel(), "EPOS.UI", "Bausteine", "Ueberlagerung.razor"));
        Assert.Contains("FocusAsync(preventScroll: true)", text);
        Assert.Empty(Ungeschuetzt(text));

        string ohneSchutz = text.Replace("FocusAsync(preventScroll: true)", "FocusAsync()");
        Assert.NotEmpty(Ungeschuetzt(ohneSchutz));
    }

    [Theory]
    [InlineData("await _wurzel.FocusAsync();", 1)]
    [InlineData("await _wurzel.FocusAsync(preventScroll: false);", 1)]
    [InlineData("await _wurzel.FocusAsync(preventScroll: true);", 0)]
    [InlineData("// Rollen gewollt: Sprung zum Fehlerfeld\nawait _feld.FocusAsync();", 0)]
    [InlineData("await _feld.FocusAsync(); // Rollen gewollt: Tastaturnavigation", 0)]
    [InlineData("// Rollen gewollt:\nawait _feld.FocusAsync();", 1)]
    [InlineData("// Rollen gewollt: Grund\n\nawait _feld.FocusAsync();", 1)]
    public void Vermerk_und_Schutz_werden_erkannt(string quelltext, int erwartet)
        => Assert.Equal(erwartet, Ungeschuetzt(quelltext).Count);

    private static IEnumerable<string> Dateien(string ordner)
        => Directory.EnumerateFiles(ordner, "*.*", SearchOption.AllDirectories)
            .Where(p => p.EndsWith(".razor", StringComparison.Ordinal) || p.EndsWith(".cs", StringComparison.Ordinal))
            .Where(p =>
            {
                string[] teile = p.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                return !teile.Contains("bin") && !teile.Contains("obj");
            });

    /// <summary>Der Weg zur Repowurzel — dasselbe Verfahren wie <c>KiKnopfEinmalWacheTests</c>.</summary>
    private static string Wurzel()
    {
        DirectoryInfo? d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "EPOS.UI", "wwwroot", "epos-ui.css")))
            d = d.Parent;

        Assert.NotNull(d);
        return d!.FullName;
    }
}
