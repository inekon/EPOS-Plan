using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using EPOS.UI.Dienste;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// <b>Die Hülle der Platzhalteranzeige</b> (<c>EPOS.UI.Daten/Bericht/VorlagenfeldanzeigeHuelle.cs</c>,
/// Konzept Berichtsvorlagen 5.6, 9.4, 9.6): nur Schlüssel mit <c>Seit</c> ≤ Katalogfassung, jeder
/// genau einmal, Beschreibung in der Sprache der Oberfläche, die drei Excel-Fälle; das Einhängen als
/// Quelle des Halters.
/// </summary>
[Collection("Vorlagenfeldhalter")]
public sealed class VorlagenfeldanzeigeHuelleTests : IDisposable
{
    private readonly Kulturvorrichtung _kultur = new();

    public void Dispose()
    {
        Vorlagenfeldhalter.Zuruecksetzen();
        _kultur.Dispose();
    }

    [Fact]
    public void Die_Huelle_fuehrt_genau_die_Schluessel_dieser_Katalogfassung()
    {
        var anzeige = VorlagenfeldanzeigeHuelle.Bilden();
        var soll = Vorlagenfeldkatalog.Alle.Where(f => f.Seit <= Vorlagenfeldkatalog.KATALOGFASSUNG)
                                          .Select(f => f.Schluessel).ToList();

        Assert.Equal(soll, anzeige.Select(a => a.Schluessel));
        Assert.Equal(anzeige.Count, anzeige.Select(a => a.Schluessel).Distinct(StringComparer.Ordinal).Count());
        Assert.All(anzeige, a => Assert.False(string.IsNullOrWhiteSpace(a.Beschreibung), a.Schluessel));
        Assert.All(anzeige, a => Assert.True(Enum.TryParse<Vorlagenfeldart>(a.ArtKennung, out _), a.Schluessel));
        Assert.All(anzeige, a => Assert.True(Enum.TryParse<Vorlagenfeldkontext>(a.KontextKennung, out _), a.Schluessel));
    }

    [Fact]
    public void Excel_nennt_Namen_Listenzeile_oder_Diagramm_und_Kapitel_bleiben_Word()
    {
        var alle = VorlagenfeldanzeigeHuelle.Bilden();
        foreach (Vorlagenfeld f in Vorlagenfeldkatalog.Alle.Where(f => f.Seit <= Vorlagenfeldkatalog.KATALOGFASSUNG))
        {
            Vorlagenfeldanzeige a = alle.First(x => x.Schluessel == f.Schluessel);
            if ((f.Ausgaben & Vorlagenausgabe.Excel) == 0) Assert.Equal("", a.Excel);
            else if (f.Art == Vorlagenfeldart.Bild) Assert.Equal("in Excel als Diagramm auf dem Tabellenbereich", a.Excel);
            else if (f.Kontext is Vorlagenfeldkontext.Stand or Vorlagenfeldkontext.Gebaeude)
                Assert.Equal("in Excel nur als Listenzeile", a.Excel);
            else Assert.Equal("EPOS." + f.Schluessel, a.Excel);
        }
        Assert.Equal("", alle.First(a => a.ArtKennung == "Kapitel").Excel);
    }

    [Fact]
    public void Die_Beschreibung_steht_in_der_Sprache_der_Oberflaeche()
    {
        Vorlagenfeld kunde = Vorlagenfeldkatalog.Finde("projekt.kunde")!;
        Vorlagenfeldanzeige de = VorlagenfeldanzeigeHuelle.Anzeige(kunde, englisch: false);
        Vorlagenfeldanzeige en = VorlagenfeldanzeigeHuelle.Anzeige(kunde, englisch: true);

        Assert.Equal(Vorlagenfeldkatalog.Beschreibung(kunde, false), de.Beschreibung);
        Assert.Equal(Vorlagenfeldkatalog.Beschreibung(kunde, true), en.Beschreibung);
        Assert.NotEqual(de.Beschreibung, en.Beschreibung);
        Assert.Equal("Text", de.ArtKennung);
    }

    [Fact]
    public void Eingehaengt_liefert_der_Halter_den_Katalog()
    {
        VorlagenfeldanzeigeHuelle.Einhaengen();
        Assert.NotNull(Vorlagenfeldhalter.Finde("projekt.kunde"));
        Assert.Equal(VorlagenfeldanzeigeHuelle.Bilden().Count, Vorlagenfeldhalter.Alle.Count);
    }
}

/// <summary>
/// <b>Die Stilregeln der Platzhalteranzeige als REGEL</b> (bunit misst weder Farbe noch Maß,
/// <c>EPOS.UI/CLAUDE.md</c>): 44-px-Trefferflächen an Marke und Stellung, nur Tokens statt
/// Farbwerten, der eigene Block für hohen Kontrast, die Schließfläche mit drei z-Ebenen.
/// </summary>
public sealed class VorlagenfeldStilblattTests
{
    private static string Blatt()
    {
        string? d = AppContext.BaseDirectory;
        while (d is not null && !File.Exists(Path.Combine(d, "WP-Plan.sln"))) d = Path.GetDirectoryName(d);
        Assert.NotNull(d);
        return File.ReadAllText(Path.Combine(d!, "EPOS.UI", "wwwroot", "epos-ui.css"));
    }

    private static string Regel(string css, string selektor)
    {
        Match m = Regex.Match(css, "(?m)^" + Regex.Escape(selektor) + @"\s*\{(?<k>[^}]*)\}");
        Assert.True(m.Success, "Regel fehlt: " + selektor);
        return m.Groups["k"].Value;
    }

    [Theory]
    [InlineData(".epos-vorlagenfeld-marke")]
    [InlineData(".epos-vorlagenfeld-stellung")]
    public void Marke_und_Stellung_sind_volle_Beruehrungsziele(string selektor)
    {
        string k = Regel(Blatt(), selektor);
        Assert.Contains("min-width: var(--epos-touchziel)", k);
        Assert.Contains("min-height: var(--epos-touchziel)", k);
    }

    [Fact]
    public void Die_Regeln_nehmen_nur_Tokens_und_kennen_hohen_Kontrast()
    {
        string css = Blatt();
        int anfang = css.IndexOf("PLATZHALTER IN DER APP", StringComparison.Ordinal);
        Assert.True(anfang > 0);
        string block = css.Substring(anfang);

        // Keine Farbe als Wert: kein #rgb, kein rgb() außer dem Hausschatten der Aufklappungen.
        Assert.DoesNotMatch(@"#[0-9a-fA-F]{3,6}\b", block);
        Assert.Equal(1, Regex.Matches(block, @"rgba?\(").Count);
        Assert.Contains("box-shadow: 0 2px 8px rgba(0, 0, 0, 0.18)", block);   // wie .epos-farbwahl & Co.
        Assert.DoesNotContain("var(--epos-marke,", block);                    // kein Rückfall in der Regel

        int kontrast = block.IndexOf("@media (forced-colors: active)", StringComparison.Ordinal);
        Assert.True(kontrast > 0, "eigener forced-colors-Block fehlt");
        string hk = block.Substring(kontrast);
        Assert.Contains(".epos-vorlagenfeld-bild", hk);
        Assert.Contains(".epos-vorlagenfeld-stellung--an", hk);
        Assert.Contains("Highlight", hk);
    }

    [Fact]
    public void Die_Schliessflaeche_liegt_unter_der_angehefteten_Marke()
    {
        string css = Blatt();
        string flaeche = Regel(css, ".epos-vorlagenfeld-schliessflaeche");
        Assert.Contains("position: fixed", flaeche);
        Assert.Contains("inset: 0", flaeche);
        Assert.Contains("z-index: 39", flaeche);
        Assert.Contains("z-index: 40", Regel(css, ".epos-vorlagenfeld--offen"));
    }
}
