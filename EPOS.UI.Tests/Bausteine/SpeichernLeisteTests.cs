using System.Globalization;
using Bunit;
using EPOS.UI.Bausteine;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// SpeichernLeiste - Regeln und Texte aus Allgemein/SpeichernLeiste.cs.
///
/// Die Leiste zieht ihre Texte aus MyResource.Resource (ADM_*). Zwei Tests pruefen
/// den deutschen Wortlaut; die UI-Kultur wird deshalb je Test auf de-DE gesetzt und
/// danach zurueckgestellt - die CI-Laeufer auf macOS und Windows laufen englisch,
/// Ubuntu und die Entwicklungsumgebung zufaellig deutsch (Befund 03.09.2026).
/// </summary>
public class SpeichernLeisteTests : BunitContext
{
    private readonly CultureInfo _kulturVorher = CultureInfo.CurrentUICulture;

    public SpeichernLeisteTests()
    {
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");
    }

    protected override void Dispose(bool disposing)
    {
        CultureInfo.CurrentUICulture = _kulturVorher;
        base.Dispose(disposing);
    }

    [Fact]
    public void OK_meldet_true()
    {
        bool? ergebnis = null;
        var cut = Render<SpeichernLeiste>(p => p.Add(x => x.Ergebnis, (bool ok) => ergebnis = ok));

        cut.Find(".epos-knopf--primaer").Click();

        Assert.True(ergebnis);
    }

    [Fact]
    public void Abbrechen_meldet_false()
    {
        bool? ergebnis = null;
        var cut = Render<SpeichernLeiste>(p => p.Add(x => x.Ergebnis, (bool ok) => ergebnis = ok));

        var knoepfe = cut.FindAll("button");
        knoepfe[0].Click();   // ohne Speichern-Knopf ist Abbrechen der erste

        Assert.False(ergebnis);
    }

    [Fact]
    public void OK_ist_gesperrt_wenn_die_Eingabe_nicht_reicht()
    {
        var cut = Render<SpeichernLeiste>(p => p.Add(x => x.OkErlaubt, false));

        Assert.True(cut.Find(".epos-knopf--primaer").HasAttribute("disabled"));
    }

    [Fact]
    public void Ohne_MitSpeichern_gibt_es_nur_zwei_Knoepfe()
    {
        var cut = Render<SpeichernLeiste>();

        Assert.Equal(2, cut.FindAll("button").Count);
    }

    [Fact]
    public void Speichern_ist_nur_bei_markiertem_Satz_UND_Aenderung_aktiv()
    {
        var cut = Render<SpeichernLeiste>(p => p
            .Add(x => x.MitSpeichern, true)
            .Add(x => x.SatzMarkiert, true)
            .Add(x => x.Geaendert, false));

        Assert.False(cut.Instance.SpeichernErlaubt);
        Assert.True(cut.FindAll("button")[0].HasAttribute("disabled"));

        cut.Render(p => p
            .Add(x => x.MitSpeichern, true)
            .Add(x => x.SatzMarkiert, true)
            .Add(x => x.Geaendert, true));

        Assert.True(cut.Instance.SpeichernErlaubt);
        Assert.False(cut.FindAll("button")[0].HasAttribute("disabled"));
    }

    [Fact]
    public void Der_Kurztext_nennt_den_Grund_der_Sperre()
    {
        var cut = Render<SpeichernLeiste>(p => p
            .Add(x => x.MitSpeichern, true)
            .Add(x => x.SatzMarkiert, false));

        // ADM_TIP_SPEICHERN_LEER: "Kein Datensatz markiert - ..."
        Assert.Contains("Datensatz", cut.FindAll("button")[0].GetAttribute("title") ?? "");
    }

    [Fact]
    public void Speichern_meldet_ohne_zu_schliessen()
    {
        int gerufen = 0;
        bool? ergebnis = null;
        var cut = Render<SpeichernLeiste>(p => p
            .Add(x => x.MitSpeichern, true)
            .Add(x => x.SatzMarkiert, true)
            .Add(x => x.Geaendert, true)
            .Add(x => x.Gespeichertwerden, () => gerufen++)
            .Add(x => x.Ergebnis, (bool ok) => ergebnis = ok));

        cut.FindAll("button")[0].Click();

        Assert.Equal(1, gerufen);
        Assert.Null(ergebnis);   // der Dialog bleibt offen
    }

    [Fact]
    public void Gespeichert_und_Fehler_schreiben_die_Statuszeile()
    {
        var cut = Render<SpeichernLeiste>();

        Assert.Equal("", cut.Find(".epos-status").TextContent);

        cut.InvokeAsync(() => cut.Instance.Gespeichert());
        Assert.Contains("Gespeichert", cut.Find(".epos-status").TextContent);
        Assert.DoesNotContain("epos-status--fehler", cut.Find(".epos-status").ClassName);

        cut.InvokeAsync(() => cut.Instance.Fehler());
        Assert.True(cut.Instance.StatusIstFehler);
        Assert.Contains("epos-status--fehler", cut.Find(".epos-status").ClassName);

        cut.InvokeAsync(() => cut.Instance.Leeren());
        Assert.Equal("", cut.Find(".epos-status").TextContent);
    }
}
