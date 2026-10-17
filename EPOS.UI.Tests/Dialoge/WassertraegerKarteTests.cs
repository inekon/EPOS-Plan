using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Kosten;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Welle KB-7c1 — der Wasserträger in der Trägerkarte</b> (Entwurf
/// <c>Dialoge/2026-10-16_Entwurf_KB7_Wasserkosten_Rueckkuehlwerk.md</c>, Abschnitt 5; Entscheide F1 und F5): Arbeitspreis
/// in €/m³ samt Szenarioknopf, keine Preisbasis-Liste, kein Heizwert-, Leistungs- oder Grundpreisfeld, kein
/// Emissions- und kein Einheitenblock. Gegenprobe: Erdgas zeigt alles, mit Preisbasis €/Nm³. Die KI-Sicht folgt
/// der Karte — ein ausgeblendetes Feld bleibt beim Setzen unberührt.
/// </summary>
public class WassertraegerKarteTests : EposBunitContext
{
    public WassertraegerKarteTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static readonly EnergietraegerDialog.EnergietraegerListe[] LISTE =
    {
        new(null, "Gas"),
        new(63, "Erdgas E"),
        new(null, "Wasser"),
        new(78, "Wasser", "Wasser der Rückkühlwerke — nicht zugeordnet", false)
    };

    private static EnergietraegerStand Wasser() => new()
    {
        TraegerZeile = "Wasser  (VDI 3805 Wasser)",
        GruppeZeile = "Gruppe: Wasser",
        TraegerName = "Wasser",
        Arbeitspreis = 2.5,
        Grundpreis = 0,
        MitHeizwert = false,
        MitBrennwert = false,
        MitLeistungspreis = false,
        MitFormel = false,
        MitGrundpreis = false,
        MitEmissionen = false,
        MitUmrechnung = false,
        MitSzenario = true,
        MitKatalogUebernahme = true,
        EinheitArbeitspreis = "€/m³",
        EinheitGrundpreis = "€/a",
        Basiseinheit = "m³",
        Preisbasen = new[] { (0, "€/m³") },
        PreisbasisId = 0,
        AltCO2 = 0,
        GueltigAb = new DateOnly(2026, 10, 16)
    };

    private static EnergietraegerStand Erdgas() => new()
    {
        TraegerZeile = "Erdgas E  (VDI 3805 3)",
        GruppeZeile = "Gruppe: Gas",
        TraegerName = "Erdgas E",
        Arbeitspreis = 0.735,
        Grundpreis = 120,
        Leistungspreis = 12,
        Heizwert = 10.5,
        Brennwert = 11.6,
        MitHeizwert = true,
        MitBrennwert = true,
        MitLeistungspreis = true,
        MitFormel = true,
        MitSzenario = true,
        EinheitArbeitspreis = "€/Nm³",
        EinheitHeizwert = "kWh/Nm³",
        EinheitBrennwert = "kWh/Nm³",
        EinheitLeistungspreis = "€/(kW·a)",
        EinheitGrundpreis = "€/a",
        Basiseinheit = "Nm³",
        Preisbasen = new[] { (0, "€/Nm³"), (1, "€/kWh") },
        PreisbasisId = 0,
        EmissionenVerfuegbar = false,
        GueltigAb = new DateOnly(2026, 10, 16)
    };

    private EnergietraegerAnsicht _ansicht = new();

    private IRenderedComponent<EnergietraegerDialog> Zeige(EnergietraegerStand stand, int vorwahl)
    {
        _ansicht = new EnergietraegerAnsicht { Stand = stand, StammName = stand.TraegerName };
        return Render<EnergietraegerDialog>(p =>
        {
            p.Add(x => x.Liste, LISTE);
            p.Add(x => x.Katalogkontext, false);
            p.Add(x => x.TraegerVorwahl, vorwahl);
            p.Add(x => x.TraegerLaden, _ => _ansicht);
            p.Add(x => x.Nachrechnen, () => _ansicht);
        });
    }

    private static string[] Feldtexte(IRenderedComponent<EnergietraegerDialog> cut) =>
        cut.FindAll(".epos-traegerkarte .epos-feld-text").Select(e => e.TextContent.Trim()).ToArray();

    private static string[] Bloecke(IRenderedComponent<EnergietraegerDialog> cut) =>
        cut.FindAll(".epos-traegerkarte .epos-gruppenkopf-titel").Select(e => e.TextContent.Trim()).ToArray();

    private static string[] Szenarioknoepfe(IRenderedComponent<EnergietraegerDialog> cut) =>
        cut.FindAll(".epos-traegerkarte .epos-szenarioknopf-text").Select(e => e.TextContent.Trim()).ToArray();

    [Fact]
    public void Der_Wassertraeger_zeigt_nur_den_Arbeitspreis_je_m3()
    {
        var cut = Zeige(Wasser(), 78);

        string[] felder = Feldtexte(cut);
        Assert.Contains("Arbeitspreis", felder);
        Assert.DoesNotContain("Grundpreis", felder);
        Assert.DoesNotContain(felder, f => f.StartsWith("Leistungspreis", StringComparison.Ordinal));
        Assert.DoesNotContain(felder, f => f.StartsWith("Heizwert", StringComparison.Ordinal));
        Assert.DoesNotContain(felder, f => f.StartsWith("Brennwert", StringComparison.Ordinal));
        Assert.Contains("€/m³", cut.FindAll(".epos-traegerkarte .epos-einheit").Select(e => e.TextContent.Trim()));

        // Die Preisbasis führt nur m³ — dann steht keine Klappliste da.
        Assert.DoesNotContain(cut.FindAll(".epos-traegerkarte select"),
                              s => s.QuerySelectorAll("option").Any(o => o.TextContent.Contains("kWh")));

        // Kein Emissions- und kein Einheitenblock; der Szenarioknopf des Arbeitspreises bleibt, der des Grundpreises fehlt.
        Assert.DoesNotContain(Bloecke(cut), b => b.StartsWith("Emissionen", StringComparison.Ordinal));
        Assert.DoesNotContain("Einheiten und Umrechnung", Bloecke(cut));
        Assert.Equal(new[] { "Arbeitspreis" }, Szenarioknoepfe(cut));
    }

    [Fact]
    public void Gegenprobe_Erdgas_zeigt_alle_Felder_mit_Nm3()
    {
        var cut = Zeige(Erdgas(), 63);

        string[] felder = Feldtexte(cut);
        Assert.Contains("Arbeitspreis", felder);
        Assert.Contains("Grundpreis", felder);
        Assert.Contains(felder, f => f.StartsWith("Leistungspreis", StringComparison.Ordinal));
        Assert.Contains(Bloecke(cut), b => b.StartsWith("Emissionen", StringComparison.Ordinal));
        Assert.Contains("Einheiten und Umrechnung", Bloecke(cut));
        IElement liste = cut.FindAll(".epos-traegerkarte select")
                            .Single(s => s.QuerySelectorAll("option").Any(o => o.TextContent.Trim() == "€/Nm³"));
        Assert.Equal(2, liste.QuerySelectorAll("option").Length);
        Assert.Contains("Grundpreis", Szenarioknoepfe(cut));
    }

    [Fact]
    public void Die_Liste_markiert_den_noch_nicht_zugeordneten_Wassertraeger()
    {
        var cut = Zeige(Wasser(), 78);
        IElement eintrag = cut.FindAll(".epos-traeger-eintrag").Last();
        Assert.Equal("Wasser der Rückkühlwerke — nicht zugeordnet", eintrag.GetAttribute("title"));
        Assert.Contains("epos-traeger-eintrag--offen", eintrag.ClassName);
        Assert.DoesNotContain("epos-traeger-eintrag--offen", cut.FindAll(".epos-traeger-eintrag").First().ClassName);
    }

    [Fact]
    public void Die_KI_Sicht_laesst_ausgeblendete_Felder_des_Wassertraegers_unberuehrt()
    {
        EnergietraegerStand wasser = Wasser();
        int nachgezogen = 0;
        var sicht = new EnergietraegerKiSicht { StandLesen = () => wasser, Nachziehen = () => nachgezogen++ };

        sicht.Arbeitspreis = 3.1;
        Assert.Equal(3.1, wasser.Arbeitspreis);
        sicht.Grundpreis = 99;
        sicht.Co2 = 5;
        sicht.EmissionenAlsCo2e = true;
        Assert.Equal(0, wasser.Grundpreis);
        Assert.Equal(0, wasser.AltCO2);
        Assert.False(wasser.ModusCo2e);
        Assert.Equal(0, sicht.Grundpreis);

        EnergietraegerStand gas = Erdgas();
        var gegen = new EnergietraegerKiSicht { StandLesen = () => gas, Nachziehen = () => { } };
        gegen.Grundpreis = 99;
        Assert.Equal(99, gas.Grundpreis);
        Assert.Equal(99, gegen.Grundpreis);
    }
}
