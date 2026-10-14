using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using EPOS.UI.Bausteine;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// <b>Der Baustein Betriebskalenderwahl</b> (Konzept Kältebedarfsprofile 7.1, Welle KBP-1): die drei Hinweise je Lage,
/// die Wahl (0 = ohne Kalender), der Knopf „Betriebskalender…“ nur mit Delegat, und die Regel der Vorwahl nach dem
/// Neulesen (<see cref="Betriebskalenderwahl.NeuerKalender"/>).
/// </summary>
public class BetriebskalenderwahlTests : EposBunitContext
{
    private static readonly (int, string)[] ZWEI = { (4, "Werk"), (9, "Sommer") };

    private IRenderedComponent<Betriebskalenderwahl> Baustein(IReadOnlyList<(int, string)> eintraege, bool zeile,
                                                              int? auswahl = null, Action<int?>? gewaehlt = null,
                                                              Action? oeffnen = null)
        => Render<Betriebskalenderwahl>(p =>
        {
            p.Add(x => x.Eintraege, eintraege)
             .Add(x => x.ZeileGewaehlt, zeile)
             .Add(x => x.Auswahl, auswahl)
             .Add(x => x.AuswahlChanged, (int? id) => gewaehlt?.Invoke(id));
            if (oeffnen is not null) p.Add(x => x.Oeffnen, oeffnen);
        });

    [Fact]
    public void Ohne_Zeile_nennt_der_Hinweis_die_Zeile_und_die_Wahl_ist_gesperrt()
    {
        var cut = Baustein(ZWEI, zeile: false);
        Assert.Contains("Kalender gelten je Projektzeile — erst eine Zeile wählen.", cut.Markup);
        Assert.True(cut.Find("select").HasAttribute("disabled"));
    }

    [Fact]
    public void Ohne_Kalender_nennt_der_Hinweis_den_Weg()
    {
        var cut = Baustein(Array.Empty<(int, string)>(), zeile: true);
        Assert.Contains("Noch kein Betriebskalender angelegt. Mit „Betriebskalender…“ Feiertage und Ferien anlegen.", cut.Markup);
        Assert.False(cut.Find("select").HasAttribute("disabled"));
    }

    [Fact]
    public void Mit_Zeile_und_Kalendern_steht_der_Hinweis_ohne_Menueweg()
    {
        var cut = Baustein(ZWEI, zeile: true, auswahl: 9);
        Assert.Contains("Feiertage und Betriebsferien des Kalenders liegen auf dem Wochenprofil", cut.Markup);
        Assert.DoesNotContain("Profile &amp; Lastgänge", cut.Markup);
        Assert.DoesNotContain("Profile & Lastgänge", cut.Markup);
        Assert.Equal("9", cut.Find("option[selected]").GetAttribute("value"));
    }

    [Fact]
    public void Die_Wahl_meldet_die_ID_und_null_fuer_ohne_Kalender()
    {
        var gemeldet = new List<int?>();
        var cut = Baustein(ZWEI, zeile: true, gewaehlt: gemeldet.Add);
        cut.Find("select").Change("4");
        cut.Find("select").Change("0");
        Assert.Equal(new int?[] { 4, null }, gemeldet);
    }

    [Fact]
    public void Kein_Delegat_kein_Knopf_mit_Delegat_oeffnet_der_Knopf()
    {
        var ohne = Baustein(ZWEI, zeile: true);
        Assert.DoesNotContain("Betriebskalender…", ohne.FindAll("button").Select(b => b.TextContent));

        int geoeffnet = 0;
        var mit = Baustein(ZWEI, zeile: false, oeffnen: () => geoeffnet++);
        mit.FindAll("button").Single(b => b.TextContent.Trim() == "Betriebskalender…").Click();
        Assert.Equal(1, geoeffnet);   // auch ohne markierte Zeile: Kalender anlegen geht immer
    }

    [Fact]
    public void Neulesen_waehlt_den_neu_angelegten_Kalender_vor()
    {
        Assert.Equal(12, Betriebskalenderwahl.NeuerKalender(ZWEI, new[] { (4, "Werk"), (9, "Sommer"), (12, "Neu") }));
        Assert.Equal(15, Betriebskalenderwahl.NeuerKalender(ZWEI, new[] { (15, "B"), (4, "Werk"), (12, "A") }));
        Assert.Null(Betriebskalenderwahl.NeuerKalender(ZWEI, ZWEI));
        Assert.Null(Betriebskalenderwahl.NeuerKalender(ZWEI, new[] { (4, "Werk") }));   // gelöscht ist nicht neu
        Assert.Equal(1, Betriebskalenderwahl.NeuerKalender(null, new[] { (1, "Erster") }));
        Assert.Null(Betriebskalenderwahl.NeuerKalender(ZWEI, null));
    }
}
