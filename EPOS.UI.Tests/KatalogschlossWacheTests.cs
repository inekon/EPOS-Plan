using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace EPOS.UI.Tests;

/// <summary>
/// <b>Die Wache des Schlosses</b> (Entscheid AD-Q15, Anwendermeldung vom 08.10.2026): Jede
/// Oberfläche, die Katalogsätze pflegt, bietet „Schloss setzen…" und „Schloss aufheben…"
/// über denselben Baustein — die Verwaltungen über die Auswahlleiste
/// (<c>Schlossumschaltung</c>), die Projektdialoge mit eigener Katalogliste über
/// <c>Katalogschloss</c> — und jede Hülle, die einen solchen Dialog mit einem Lösch- oder
/// Speicherweg des Katalogs füttert, reicht auch den Schlossweg herein. Eine Ausnahme steht
/// nur BENANNT, mit Grund, in den beiden Listen unten.
/// </summary>
public class KatalogschlossWacheTests
{
    /// <summary>Dialoge, die Zeilen pflegen und das Schloss mit Grund NICHT anbieten.</summary>
    private static readonly IReadOnlyDictionary<string, string> DialogAusnahmen = new Dictionary<string, string>
    {
        ["TwwNutzungsartAdminDialog.razor"] =
            "Tww-Katalog: ReadOnly folgt dem Freigabestatus (KatalogDefinition.SchlossAusStatus); der Kern lehnt das Umschalten ab.",
        ["PeakShavingDialog.razor"] =
            "Lastspitzenkappung: Die Auswahlleiste wirkt auf gerechnete Varianten, nicht auf einen Katalog.",
        ["RaumnutzungBlatt.razor"] =
            "Nutzungsprofile: Ihr Katalog steht nicht im Register der Katalogfassung und nicht in der KatalogRegistry; " +
            "ausgelieferte Zeilen sind nur duplizierbar (NP-F19).",
    };

    /// <summary>Hüllen mit Katalogpflege, die den Schlossweg mit Grund NICHT hereinreichen.</summary>
    private static readonly IReadOnlyDictionary<string, string> HuellenAusnahmen = new Dictionary<string, string>();

    /// <summary>Ein Dialog pflegt Zeilen: Auswahlleiste oder ein Löschweg des Katalogs.</summary>
    private static readonly Regex DialogPflegt =
        new(@"<Auswahlleiste\b|\[Parameter\]\s+public\s+Func<[^>]*>\?\s+KatalogLoeschen\b", RegexOptions.Compiled);

    /// <summary>Der Dialog bietet das Schloss.</summary>
    private static readonly Regex DialogSchloss = new(@"<Katalogschloss\b|\bSchlossumschaltung\b", RegexOptions.Compiled);

    /// <summary>Eine Hülle füttert Katalogpflege.</summary>
    private static readonly Regex HuellePflegt =
        new(@"\[""KatalogLoeschen""\]|\[""KatalogfelderSpeichern""\]|new\s+(KatalogBrowserWege|ModulKatalogWege)\b",
            RegexOptions.Compiled);

    /// <summary>Die Hülle reicht den Schlossweg herein.</summary>
    private static readonly Regex HuelleSchloss =
        new(@"\[""Schloss""\]\s*=\s*Schlosswege\.Aus\(|\bSchloss\s*=\s*Schlosswege\.Aus\(", RegexOptions.Compiled);

    /// <summary>
    /// Eine Verwaltung OHNE Auswahlleiste: Sie zeichnet das Kennzeichen eines Auslieferungssatzes und
    /// bietet Löschen an (Vorbild: die Vorlagenverwaltung der Konditionierung).
    /// </summary>
    private static readonly Regex Kennzeichen = new(@"<Kennzeichen\b", RegexOptions.Compiled);
    private static readonly Regex Loeschen = new(@"\bLoeschen\b", RegexOptions.Compiled);

    internal static bool DialogOhneSchloss(string text)
        => (DialogPflegt.IsMatch(text) || (Kennzeichen.IsMatch(text) && Loeschen.IsMatch(text)))
           && !DialogSchloss.IsMatch(text);

    internal static bool HuelleOhneSchloss(string text) => HuellePflegt.IsMatch(text) && !HuelleSchloss.IsMatch(text);

    [Fact]
    public void Jeder_pflegende_Dialog_bietet_das_Schloss()
    {
        string wurzel = Wurzel();
        var treffer = Dateien(Path.Combine(wurzel, "EPOS.UI", "Dialoge"), ".razor")
            .Concat(Dateien(Path.Combine(wurzel, "EPOS.UI", "Seiten"), ".razor"))
            .Where(p => !DialogAusnahmen.ContainsKey(Path.GetFileName(p)))
            .Where(p => DialogOhneSchloss(File.ReadAllText(p)))
            .Select(p => Path.GetRelativePath(wurzel, p))
            .ToList();
        Assert.True(treffer.Count == 0, "Ohne Schloss: " + string.Join(", ", treffer));
    }

    [Fact]
    public void Jede_Huelle_mit_Katalogpflege_reicht_den_Schlossweg_herein()
    {
        string wurzel = Wurzel();
        var treffer = Dateien(Path.Combine(wurzel, "WindowsFormsApplication1", "Views"), ".cs")
            .Concat(Dateien(Path.Combine(wurzel, "EPOS.UI.Daten"), ".cs"))
            .Where(p => !HuellenAusnahmen.ContainsKey(Path.GetFileName(p)))
            .Where(p => HuelleOhneSchloss(File.ReadAllText(p)))
            .Select(p => Path.GetRelativePath(wurzel, p))
            .ToList();
        Assert.True(treffer.Count == 0, "Ohne Schlossweg: " + string.Join(", ", treffer));
    }

    [Fact]
    public void Die_Ausnahmen_gibt_es_und_sie_tragen_einen_Grund()
    {
        string wurzel = Wurzel();
        var dialoge = Dateien(Path.Combine(wurzel, "EPOS.UI"), ".razor").Select(Path.GetFileName).ToHashSet();
        foreach (var (datei, grund) in DialogAusnahmen)
        {
            Assert.Contains(datei, dialoge);
            Assert.False(string.IsNullOrWhiteSpace(grund));
        }
    }

    /// <summary>Gegenprobe: Die Muster erkennen den Verstoß und lassen den richtigen Bau durch.</summary>
    [Theory]
    [InlineData("<Auswahlleiste Handlungen=\"@H\" />", true)]
    [InlineData("<Auswahlleiste Handlungen=\"@H\" />\nprivate readonly Schlossumschaltung _s = new();", false)]
    [InlineData("[Parameter] public Func<int, string>? KatalogLoeschen { get; set; }", true)]
    [InlineData("[Parameter] public Func<int, string>? KatalogLoeschen { get; set; }\n<Katalogschloss Weg=\"@Schloss\" />", false)]
    [InlineData("<Katalogliste Zeilen=\"@_z\" />", false)]
    [InlineData("<Kennzeichen Kurztext=\"@K\" />\n<button @onclick=\"() => Loeschen(v)\">x</button>", true)]
    [InlineData("<Kennzeichen Kurztext=\"@K\" />\n@onclick=\"() => Loeschen(v)\"\nprivate readonly Schlossumschaltung _s = new();", false)]
    [InlineData("<Kennzeichen Kurztext=\"@K\" />", false)]
    public void Gegenprobe_Dialog(string text, bool verstoss) => Assert.Equal(verstoss, DialogOhneSchloss(text));

    [Theory]
    [InlineData("[\"KatalogLoeschen\"] = new Func<int, bool>(id => stamm.Delete(id)),", true)]
    [InlineData("[\"KatalogLoeschen\"] = x,\n[\"Schloss\"] = Schlosswege.Aus(BHKWStammCtrl.SchlossSetzen),", false)]
    [InlineData("return new KatalogBrowserWege { Loeschen = Loeschen };", true)]
    [InlineData("return new KatalogBrowserWege { Schloss = Schlosswege.Aus(X.SchlossSetzen) };", false)]
    [InlineData("[\"Katalogzeilen\"] = z,", false)]
    public void Gegenprobe_Huelle(string text, bool verstoss) => Assert.Equal(verstoss, HuelleOhneSchloss(text));

    private static IEnumerable<string> Dateien(string ordner, string endung)
        => Directory.EnumerateFiles(ordner, "*" + endung, SearchOption.AllDirectories)
            .Where(p =>
            {
                string[] teile = p.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                return !teile.Contains("bin") && !teile.Contains("obj");
            });

    private static string Wurzel()
    {
        DirectoryInfo? d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "EPOS.UI", "wwwroot", "epos-ui.css")))
            d = d.Parent;
        Assert.NotNull(d);
        return d!.FullName;
    }
}
