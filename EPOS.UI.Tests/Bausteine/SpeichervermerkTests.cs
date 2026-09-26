using Bunit;
using EPOS.UI.Bausteine;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// Speichervermerk — Speichern-Knopf mit Rückmeldung daneben: „Gespeichert um …",
/// roter Fehlertext, weiche Sperre ohne Änderung, harte Sperre bei Fehleingabe, und
/// der Vermerk fällt mit der nächsten Änderung oder einem Satzwechsel weg.
/// </summary>
public class SpeichervermerkTests : EposBunitContext
{
    private IRenderedComponent<Speichervermerk> Aufbauen(
        bool geaendert = true, bool gesperrt = false, string? satz = "A", Action? speichern = null)
        => Render<Speichervermerk>(p => p
            .Add(x => x.Text, "Felder speichern")
            .Add(x => x.Geaendert, geaendert)
            .Add(x => x.Gesperrt, gesperrt)
            .Add(x => x.Satz, satz)
            .Add(x => x.Speichern, () => speichern?.Invoke()));

    [Fact]
    public void Ohne_Vermerk_zeichnet_nur_der_Knopf()
    {
        var cut = Aufbauen();

        Assert.Equal("Felder speichern", cut.Find("button").TextContent);
        Assert.Empty(cut.FindAll("[role=status]"));
        Assert.False(cut.Find("button").HasAttribute("aria-disabled"));
        Assert.False(cut.Find("button").HasAttribute("disabled"));
    }

    [Fact]
    public void Klick_mit_Aenderung_ruft_den_Wirt()
    {
        int n = 0;
        var cut = Aufbauen(speichern: () => n++);

        cut.Find("button").Click();

        Assert.Equal(1, n);
    }

    [Fact]
    public void Gespeichert_zeigt_die_Uhrzeit_am_Knopf()
    {
        var cut = Aufbauen();

        cut.InvokeAsync(() => cut.Instance.Gespeichert());

        var status = cut.Find(".epos-speichervermerk [role=status]");
        Assert.StartsWith("Gespeichert um ", status.TextContent);
        Assert.DoesNotContain("epos-status--fehler", status.ClassName);
        Assert.False(cut.Instance.VermerkIstFehler);
    }

    [Fact]
    public void Fehler_steht_rot_am_Knopf()
    {
        var cut = Aufbauen();

        cut.InvokeAsync(() => cut.Instance.Fehler("Wert unzulässig."));

        var status = cut.Find("[role=status]");
        Assert.Equal("Wert unzulässig.", status.TextContent);
        Assert.Contains("epos-status--fehler", status.ClassName);

        cut.InvokeAsync(() => cut.Instance.Fehler());
        Assert.Equal("Nicht gespeichert", cut.Find("[role=status]").TextContent);
    }

    [Fact]
    public void Ohne_Aenderung_weich_gesperrt_und_Klick_nennt_den_Grund()
    {
        int n = 0;
        var cut = Aufbauen(geaendert: false, speichern: () => n++);

        var knopf = cut.Find("button");
        Assert.Equal("true", knopf.GetAttribute("aria-disabled"));
        Assert.False(knopf.HasAttribute("disabled"));
        Assert.Equal(Speichervermerk.KurztextUnveraendert, knopf.GetAttribute("title"));

        knopf.Click();

        Assert.Equal(0, n);
        Assert.Equal(Speichervermerk.KurztextUnveraendert, cut.Find("[role=status]").TextContent);
    }

    [Fact]
    public void Harte_Sperre_ist_disabled_und_ruft_nicht()
    {
        var cut = Aufbauen(gesperrt: true);

        Assert.True(cut.Find("button").HasAttribute("disabled"));
        Assert.False(cut.Find("button").HasAttribute("aria-disabled"));
    }

    [Fact]
    public void Neue_Aenderung_nimmt_den_Vermerk_zurueck()
    {
        var cut = Aufbauen();
        cut.InvokeAsync(() => cut.Instance.Gespeichert());
        cut.Render(p => p.Add(x => x.Geaendert, false));
        Assert.NotEmpty(cut.FindAll("[role=status]"));

        cut.Render(p => p.Add(x => x.Geaendert, true));

        Assert.Empty(cut.FindAll("[role=status]"));
    }

    [Fact]
    public void Satzwechsel_nimmt_den_Vermerk_zurueck()
    {
        var cut = Aufbauen(geaendert: false);
        cut.InvokeAsync(() => cut.Instance.Fehler("x"));

        cut.Render(p => p.Add(x => x.Satz, "B"));

        Assert.Empty(cut.FindAll("[role=status]"));
    }

    [Fact]
    public void Leeren_nimmt_auch_einen_Fehler_zurueck()
    {
        var cut = Aufbauen();
        cut.InvokeAsync(() => cut.Instance.Fehler("x"));

        cut.InvokeAsync(() => cut.Instance.Leeren());

        Assert.Empty(cut.FindAll("[role=status]"));
        Assert.Equal("", cut.Instance.Vermerk);
    }
}
