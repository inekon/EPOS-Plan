using System.Globalization;
using Bunit;
using EPOS.UI.Bausteine;
using Microsoft.AspNetCore.Components;
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
public class SpeichernLeisteTests : EposBunitContext
{
    public SpeichernLeisteTests()
    {
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

    /// <summary>
    /// Die WEICHE Sperre (Stufe G4c, Gebäudeimport): Mit Grund trägt OK <c>aria-disabled</c> und
    /// den Grund als <c>title</c>, bleibt aber anklickbar — der Dialog meldet den Versuch. Ohne
    /// Grund steht keines der beiden Attribute im Markup.
    /// </summary>
    [Fact]
    public void OK_mit_Sperrgrund_ist_weich_gesperrt_und_meldet_weiter()
    {
        bool? ergebnis = null;
        var cut = Render<SpeichernLeiste>(p => p
            .Add(x => x.OkSperrgrund, "Noch nichts gelesen")
            .Add(x => x.Ergebnis, (bool ok) => ergebnis = ok));

        var ok = cut.Find(".epos-knopf--primaer");
        Assert.Equal("true", ok.GetAttribute("aria-disabled"));
        Assert.Equal("Noch nichts gelesen", ok.GetAttribute("title"));
        Assert.False(ok.HasAttribute("disabled"));

        ok.Click();
        Assert.True(ergebnis);

        var ohne = Render<SpeichernLeiste>().Find(".epos-knopf--primaer");
        Assert.False(ohne.HasAttribute("aria-disabled"));
        Assert.False(ohne.HasAttribute("title"));
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

        // Weich gesperrt: aria-disabled statt disabled, damit der Kurztext erscheint.
        Assert.False(cut.Instance.SpeichernErlaubt);
        Assert.Equal("true", cut.FindAll("button")[0].GetAttribute("aria-disabled"));
        Assert.False(cut.FindAll("button")[0].HasAttribute("disabled"));

        cut.Render(p => p
            .Add(x => x.MitSpeichern, true)
            .Add(x => x.SatzMarkiert, true)
            .Add(x => x.Geaendert, true));

        Assert.True(cut.Instance.SpeichernErlaubt);
        Assert.False(cut.FindAll("button")[0].HasAttribute("aria-disabled"));
        Assert.False(cut.FindAll("button")[0].HasAttribute("disabled"));
    }

    /// <summary>
    /// Die weiche Sperre von Speichern: Ein Klick ohne Änderung ruft den Dialog NICHT, er
    /// setzt den Grund (<c>ADM_TIP_SPEICHERN_UNVERAENDERT</c>) in die Statusspanne.
    /// </summary>
    [Fact]
    public void Speichern_ohne_Aenderung_nennt_den_Grund_statt_zu_rufen()
    {
        int gerufen = 0;
        var cut = Render<SpeichernLeiste>(p => p
            .Add(x => x.MitSpeichern, true)
            .Add(x => x.SatzMarkiert, true)
            .Add(x => x.Geaendert, false)
            .Add(x => x.Gespeichertwerden, () => gerufen++));

        cut.FindAll("button")[0].Click();

        Assert.Equal(0, gerufen);
        Assert.Equal(WindowsFormsApplication1.MyResource.Resource.ADM_TIP_SPEICHERN_UNVERAENDERT, cut.Instance.StatusText);
        Assert.False(cut.Instance.StatusIstFehler);
        Assert.Equal(WindowsFormsApplication1.MyResource.Resource.ADM_TIP_SPEICHERN_UNVERAENDERT, cut.Find(".epos-status").TextContent);
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

    [Fact]
    public void Ohne_MitAbbrechen_bleiben_Speichern_und_OK()
    {
        // Eine Maske, die schon beim Speichern schreibt, kennt kein Verwerfen mehr -
        // der Dialog "BHKW-Wirtschaftlichkeit" hat nur "Speichern" und "Schliessen".
        var cut = Render<SpeichernLeiste>(p => p
            .Add(x => x.MitSpeichern, true)
            .Add(x => x.MitAbbrechen, false)
            .Add(x => x.OkText, "Schließen"));

        var knoepfe = cut.FindAll(".epos-leiste button");
        Assert.Equal(2, knoepfe.Count);
        Assert.Equal("Speichern", knoepfe[0].TextContent);
        Assert.Equal("Schließen", knoepfe[1].TextContent);
    }

    // =====================================================================
    //  Der linke Aktionsschlitz (DL-2, Schritt 0)
    // =====================================================================

    /// <summary>
    /// Die Reihenfolge der Fussleiste ist Aktionen · Status (= Fueller) ·
    /// [Speichern] · Abbrechen · OK. Der Schlitz steht also VOR dem Statustext -
    /// und damit links vom Fueller, denn die Statusspanne IST der Fueller
    /// (<c>.epos-status { flex: 1 1 auto }</c>).
    /// </summary>
    [Fact]
    public void Der_Aktionsschlitz_steht_vor_dem_Status_und_damit_vor_dem_Fueller()
    {
        var cut = Render<SpeichernLeiste>(p => p
            .Add(x => x.MitSpeichern, true)
            .Add(x => x.SatzMarkiert, true)
            .Add(x => x.Geaendert, true)
            .Add(x => x.Aktionen, (RenderFragment)(b =>
            {
                b.OpenElement(0, "button");
                b.AddAttribute(1, "type", "button");
                b.AddAttribute(2, "class", "epos-knopf");
                b.AddContent(3, "Standardwerte");
                b.CloseElement();
            })));

        var leiste = cut.Find(".epos-leiste");
        var kinder = leiste.Children;

        // Erstes Kind ist der Aktionsknopf, zweites die Statusspanne.
        Assert.Equal("BUTTON", kinder[0].TagName);
        Assert.Equal("Standardwerte", kinder[0].TextContent);
        Assert.Contains("epos-status", kinder[1].ClassName);

        // Und in der Knopffolge steht er vor Speichern, Abbrechen und OK.
        var knoepfe = cut.FindAll(".epos-leiste button");
        Assert.Equal(4, knoepfe.Count);
        Assert.Equal("Standardwerte", knoepfe[0].TextContent);
        Assert.Equal("Speichern", knoepfe[1].TextContent);
        Assert.Equal("Abbrechen", knoepfe[2].TextContent);
        Assert.Contains("epos-knopf--primaer", knoepfe[3].ClassName);
    }

    /// <summary>
    /// Der Bestandstest zum Schlitz: OHNE <c>Aktionen</c> zeichnet die Leiste
    /// genau dasselbe Markup wie vor DL-2 — kein Platzhalter, kein leeres
    /// Element, die Statusspanne bleibt das erste Kind. Die bestehenden Aufrufer
    /// bleiben deshalb unveraendert.
    /// </summary>
    [Fact]
    public void Ohne_Aktionen_bleibt_das_Markup_unveraendert()
    {
        var cut = Render<SpeichernLeiste>();

        var leiste = cut.Find(".epos-leiste");

        Assert.Equal(3, leiste.Children.Length);          // Status, Abbrechen, OK
        Assert.Contains("epos-status", leiste.Children[0].ClassName);
        Assert.Equal("Abbrechen", leiste.Children[1].TextContent);
        Assert.Equal("OK", leiste.Children[2].TextContent);
        Assert.Contains("epos-knopf--primaer", leiste.Children[2].ClassName);

        // Kein zusaetzlicher Knoten: Die Leiste zaehlt genau die drei Kinder von
        // vor DL-2, und der Schlitz hinterlaesst keine leere Huelse.
        Assert.Equal(2, cut.FindAll(".epos-leiste button").Count);
    }
}
