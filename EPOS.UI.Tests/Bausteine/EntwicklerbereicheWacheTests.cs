using System;
using System.Collections.Generic;
using System.Linq;
using EPOS.UI.Bausteine;
using EPOS.UI.Seiten;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// Die Wache der Entwicklerbereiche, Teil 1: das Menue (Administration und Support, Stufe 1;
/// Konzept Abschnitt 3.1, Entwicklerhandbuch Abschnitte 2.2 und 2.3). Der Baum des Anwenders
/// zeigt keinen Punkt der Stufe Entwickler und bleibt dabei aufgeraeumt; der Baum des
/// Entwicklers fuehrt den Kopf „Entwicklung" vor „Sprache".
/// </summary>
public class EntwicklerbereicheWacheTests
{
    /// <summary>Die sieben handelnden Punkte des Kopfes „Entwicklung" in ihrer Reihenfolge.</summary>
    private static readonly string[] EntwicklungPunkte =
    {
        "MenuItem_EntwAusnahmen", "MenuItem_EntwProtokollordner", "MenuItem_EntwDatenbankordner",
        "MenuItem_EntwDiagnose", "MenuItem_EntwWebWerkzeuge", "MenuItem_EntwWiederherstellen",
        "MenuItem_EntwAus",
    };

    private static IEnumerable<Menuepunkt> Flach(IEnumerable<Menuepunkt> punkte) =>
        punkte.SelectMany(p => new[] { p }.Concat(Flach(p.Untereintraege)));

    /// <summary>Jede Ebene des Baums: die oberste und jedes Untermenue.</summary>
    private static IEnumerable<(string Ort, IReadOnlyList<Menuepunkt> Ebene)> Ebenen(IReadOnlyList<Menuepunkt> wurzel)
    {
        yield return ("(oberste Ebene)", wurzel);
        foreach (Menuepunkt p in Flach(wurzel).Where(p => p.Klappt))
            yield return (p.Name, p.Untereintraege);
    }

    [Fact]
    public void Der_Baum_des_Anwenders_zeigt_keinen_Punkt_der_Stufe_Entwickler()
    {
        List<Menuepunkt> anwender = Flach(Menuetabelle.Sichtbar(Freigabestufe.Anwender)).ToList();

        Assert.DoesNotContain(anwender, p => p.Stufe > Freigabestufe.Anwender);
        Assert.DoesNotContain(anwender, p => p.Name == "MenuItem_Entwicklung");
        // Der Anwenderbaum: 73 Punkte ohne Trenner (Stufe 2 bringt „Fernwartung…", KM4-E3-c „Kältemittel…", K5b „Kältebedarf" unter „Profile & Lastgänge").
        Assert.Equal(73, anwender.Count(p => !p.Trenner));
        foreach (string name in EntwicklungPunkte)
            Assert.DoesNotContain(anwender, p => p.Name == name);
    }

    [Theory]
    [InlineData(Freigabestufe.Anwender)]
    [InlineData(Freigabestufe.Entwickler)]
    public void Jede_Ebene_ist_aufgeraeumt(Freigabestufe stufe)
    {
        foreach ((string ort, IReadOnlyList<Menuepunkt> ebene) in Ebenen(Menuetabelle.Sichtbar(stufe)))
        {
            Assert.True(ebene.Count > 0, "leere Ebene: " + ort);
            Assert.False(ebene[0].Trenner, "fuehrender Trenner in " + ort);
            Assert.False(ebene[^1].Trenner, "schliessender Trenner in " + ort);
            for (int i = 1; i < ebene.Count; i++)
                Assert.False(ebene[i].Trenner && ebene[i - 1].Trenner, "doppelter Trenner in " + ort);
        }
    }

    [Theory]
    [InlineData(Freigabestufe.Anwender)]
    [InlineData(Freigabestufe.Entwickler)]
    public void Kein_Untermenue_ist_leer_oder_fuehrt_nur_einen_Punkt(Freigabestufe stufe)
    {
        var verstoesse = Flach(Menuetabelle.Sichtbar(stufe))
            .Where(p => p.Klappt && p.Untereintraege.Count(k => !k.Trenner) < 2)
            .Select(p => p.Name)
            .ToList();

        Assert.Empty(verstoesse);
    }

    [Fact]
    public void Der_Baum_des_Entwicklers_fuehrt_den_Kopf_Entwicklung_vor_Sprache()
    {
        IReadOnlyList<Menuepunkt> koepfe = Menuetabelle.Sichtbar(Freigabestufe.Entwickler);

        int entwicklung = koepfe.ToList().FindIndex(p => p.Name == "MenuItem_Entwicklung");
        int sprache = koepfe.ToList().FindIndex(p => p.Name == "Sprache");
        Assert.True(entwicklung >= 0, "Kopf Entwicklung fehlt");
        Assert.Equal(sprache - 1, entwicklung);

        Menuepunkt kopf = koepfe[entwicklung];
        Assert.Equal("MENU_ENTWICKLUNG", kopf.TextSchluessel);
        Assert.Equal(Freigabestufe.Entwickler, kopf.Stufe);
        Assert.True(string.IsNullOrEmpty(kopf.Ziel));

        List<Menuepunkt> handelnd = kopf.Untereintraege.Where(p => !p.Trenner).ToList();
        Assert.Equal(EntwicklungPunkte, handelnd.Select(p => p.Name).ToArray());
        Assert.All(handelnd, p => Assert.False(p.Klappt));
        Assert.All(handelnd, p => Assert.Equal(Freigabestufe.Entwickler, p.Stufe));
        Assert.Equal(2, kopf.Untereintraege.Count(p => p.Trenner));

        // „Auslieferungsstand wiederherstellen…" oeffnet den Katalogabgleich mit Argument.
        Menuepunkt wieder = handelnd.Single(p => p.Name == "MenuItem_EntwWiederherstellen");
        Assert.Equal(Seitenschluessel.Katalogabgleich, wieder.Ziel);
        Assert.Equal("wiederherstellen", wieder.Argument);
    }

    [Fact]
    public void Jeder_neue_Seitenschluessel_steht_in_Alle()
    {
        var alle = new HashSet<string>(Seitenschluessel.Alle, StringComparer.Ordinal);
        foreach (string s in new[]
                 {
                     Seitenschluessel.EntwAusnahmeprotokoll, Seitenschluessel.EntwProtokollordner,
                     Seitenschluessel.EntwDatenbankordner, Seitenschluessel.EntwDiagnosedaten,
                     Seitenschluessel.EntwWebWerkzeuge, Seitenschluessel.EntwModusAus,
                 })
            Assert.Contains(s, alle);

        // Jedes Ziel des Kopfes „Entwicklung" ist ein bekannter Schluessel.
        Menuepunkt kopf = Menuetabelle.Eintraege.Single(p => p.Name == "MenuItem_Entwicklung");
        Assert.All(kopf.Untereintraege.Where(p => !p.Trenner), p => Assert.Contains(p.Ziel, alle));
    }

    [Fact]
    public void Jeder_Text_des_Kopfes_steht_in_beiden_Sprachen()
    {
        Menuepunkt kopf = Menuetabelle.Eintraege.Single(p => p.Name == "MenuItem_Entwicklung");
        var de = new System.Globalization.CultureInfo("de-DE");
        var en = new System.Globalization.CultureInfo("en-US");
        foreach (Menuepunkt p in new[] { kopf }.Concat(kopf.Untereintraege.Where(k => !k.Trenner)))
        {
            string? textDe = WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(p.TextSchluessel, de);
            string? textEn = WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(p.TextSchluessel, en);
            Assert.False(string.IsNullOrEmpty(textDe), p.TextSchluessel + " (de)");
            Assert.False(string.IsNullOrEmpty(textEn), p.TextSchluessel + " (en)");
            Assert.NotEqual(textDe, textEn);
        }
    }
}
