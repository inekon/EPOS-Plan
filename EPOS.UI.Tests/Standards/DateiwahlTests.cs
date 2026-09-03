using Bunit;
using EPOS.UI.Standards;
using Xunit;

namespace EPOS.UI.Tests.Standards;

/// <summary>
/// Dateiwahl (iU9-W3.0) - Pfadfeld und Knopf. Die Komponente oeffnet nichts;
/// geprueft wird, dass sie den Waehler der Plattform richtig benutzt.
/// </summary>
public class DateiwahlTests : BunitContext
{
    [Fact]
    public void Ohne_Waehler_gibt_es_keinen_Knopf()
    {
        var cut = Render<Dateiwahl>(p => p.Add(x => x.Bezeichnung, "Datei:"));

        Assert.Empty(cut.FindAll("button"));
        Assert.Contains("Datei:", cut.Markup);
    }

    [Fact]
    public void Mit_Waehler_erscheint_der_Knopf_mit_seinem_Text()
    {
        var cut = Render<Dateiwahl>(p => p
            .Add(x => x.Waehlen, f => Task.FromResult<string?>(null))
            .Add(x => x.KnopfText, "Datei wählen …"));

        Assert.Equal("Datei wählen …", cut.Find("button").TextContent.Trim());
    }

    [Fact]
    public void Der_gewaehlte_Pfad_wird_gemeldet_und_angezeigt()
    {
        string? gemeldet = null;
        var cut = Render<Dateiwahl>(p => p
            .Add(x => x.Waehlen, f => Task.FromResult<string?>(@"C:\Daten\spot.csv"))
            .Add(x => x.PfadChanged, (string s) => gemeldet = s));

        cut.Find("button").Click();

        Assert.Equal(@"C:\Daten\spot.csv", gemeldet);
        Assert.Equal(@"C:\Daten\spot.csv", cut.Find("input").GetAttribute("value"));
    }

    [Fact]
    public void Der_Filter_geht_an_den_Waehler()
    {
        string? gesehen = null;
        var cut = Render<Dateiwahl>(p => p
            .Add(x => x.Filter, "CSV (*.csv)|*.csv")
            .Add(x => x.Waehlen, f => { gesehen = f; return Task.FromResult<string?>(null); }));

        cut.Find("button").Click();

        Assert.Equal("CSV (*.csv)|*.csv", gesehen);
    }

    /// <summary>Abbrechen sagt nichts und laesst den alten Pfad stehen (A-13 aus W2).</summary>
    [Fact]
    public void Ein_abgebrochener_Waehler_laesst_den_Pfad_stehen()
    {
        bool gemeldet = false;
        var cut = Render<Dateiwahl>(p => p
            .Add(x => x.Pfad, @"C:\alt.csv")
            .Add(x => x.Waehlen, f => Task.FromResult<string?>(""))
            .Add(x => x.PfadChanged, (string s) => gemeldet = true));

        cut.Find("button").Click();

        Assert.False(gemeldet);
        Assert.Equal(@"C:\alt.csv", cut.Find("input").GetAttribute("value"));
    }

    [Fact]
    public void Das_Pfadfeld_ist_in_der_Vorgabe_nur_lesbar()
    {
        var cut = Render<Dateiwahl>();

        Assert.True(cut.Find("input").HasAttribute("readonly"));
    }

    [Fact]
    public void Ein_beschreibbares_Pfadfeld_meldet_die_Eingabe()
    {
        string? gemeldet = null;
        var cut = Render<Dateiwahl>(p => p
            .Add(x => x.NurLesen, false)
            .Add(x => x.PfadChanged, (string s) => gemeldet = s));

        cut.Find("input").Input(@"D:\reihe.csv");

        Assert.Equal(@"D:\reihe.csv", gemeldet);
    }

    [Fact]
    public void Gesperrt_bleibt_der_Knopf_sichtbar()
    {
        var cut = Render<Dateiwahl>(p => p
            .Add(x => x.Aktiv, false)
            .Add(x => x.Waehlen, f => Task.FromResult<string?>(null)));

        Assert.True(cut.Find("button").HasAttribute("disabled"));
    }
}
