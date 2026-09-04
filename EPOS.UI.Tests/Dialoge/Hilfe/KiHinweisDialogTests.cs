using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bunit;
using EPOS.UI.Dialoge.Hilfe;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace EPOS.UI.Tests.Dialoge.Hilfe;

/// <summary>
/// <see cref="KiHinweisDialog"/> — Zeuge T-4 (iU9-W15b.3).
///
/// <para><b>Was hier bewiesen wird.</b> Der Rechtshinweis ist der sichtbare Teil
/// des Riegels. Er beschreibt ausschließlich den TATSÄCHLICHEN Datenfluss —
/// sieben Abschnitte: was hinausgeht, was im Aktionsbetrieb dazukommt, was
/// NICHT hinausgeht, wer Empfänger ist, was der Anwender beachten muss, wer
/// verantwortlich ist und wie sich alles abschalten lässt. Fällt ein Abschnitt
/// aus, ist die Einwilligung des Anwenders eine andere als die, die
/// <c>KiEinwilligung.FASSUNG</c> behauptet.</para>
///
/// <para>Und die Absätze müssen Absätze bleiben. Der Vorläufer musste die
/// Zeilenumbrüche der Ressourcen eigens von <c>\n</c> auf
/// <c>Environment.NewLine</c> übersetzen; geht das hier verloren, steht der
/// Rechtshinweis als Textwurst.</para>
///
/// <para>Die Klasse pinnt die Sprache selbst (Regel seit W8).</para>
/// </summary>
public class KiHinweisDialogTests : BunitContext
{
    public KiHinweisDialogTests()
    {
        var de = new CultureInfo("de-DE");
        CultureInfo.CurrentCulture = de;
        CultureInfo.CurrentUICulture = de;
        CultureInfo.DefaultThreadCurrentCulture = de;
        CultureInfo.DefaultThreadCurrentUICulture = de;
    }

    private static readonly string[] UEBERSCHRIFTEN =
    {
        "Was übertragen wird",
        "Was im Aktionsbetrieb zusätzlich übertragen wird",
        "Was nicht übertragen wird",
        "Wer Empfänger ist",
        "Was Sie beachten müssen",
        "Wer verantwortlich ist",
        "Wie Sie alles abschalten"
    };

    private static IReadOnlyList<KiHinweisAbschnitt> Abschnitte()
        => UEBERSCHRIFTEN.Select((u, i) => new KiHinweisAbschnitt(u, "Absatz " + (i + 1))).ToList();

    private IRenderedComponent<KiHinweisDialog> Zeigen(bool mitEinwilligung,
                                                       string stand = "Noch keine Einwilligung.",
                                                       EventCallback<bool>? beantwortet = null)
    {
        return Render<KiHinweisDialog>(p =>
        {
            p.Add(x => x.MitEinwilligung, mitEinwilligung)
             .Add(x => x.Titel, "Hinweis zur Datenübertragung an den KI-Dienst")
             .Add(x => x.Fassungszeile, "Fassung 2")
             .Add(x => x.Einleitung, "Der Assistent sendet Ihre Frage an einen Dienst von Google.")
             .Add(x => x.Abschnitte, Abschnitte())
             .Add(x => x.Stand, stand)
             .Add(x => x.EinverstandenText, "Verstanden und einverstanden")
             .Add(x => x.AbbrechenText, "Abbrechen")
             .Add(x => x.SchliessenText, "Schließen");

            if (beantwortet is not null) p.Add(x => x.Beantwortet, beantwortet.Value);
        });
    }

    // ==================================================================
    //  Der Text
    // ==================================================================

    /// <summary>
    /// <b>Sieben Abschnitte, in dieser Reihenfolge</b> — dazu Titel,
    /// Fassungszeile und Einleitung. Die Reihenfolge ist Teil der Aussage.
    /// </summary>
    [Fact]
    public void Sieben_Abschnitte_stehen_in_ihrer_Reihenfolge()
    {
        var cut = Zeigen(mitEinwilligung: true);

        var ueberschriften = cut.FindAll("h3.epos-kihinweis-ueberschrift")
                                .Select(h => h.TextContent).ToArray();

        Assert.Equal(UEBERSCHRIFTEN, ueberschriften);
    }

    [Fact]
    public void Titel_Fassung_und_Einleitung_stehen_ueber_den_Abschnitten()
    {
        var cut = Zeigen(mitEinwilligung: true);

        Assert.Equal("Hinweis zur Datenübertragung an den KI-Dienst",
                     cut.Find("h2.epos-kihinweis-titel").TextContent);
        Assert.Equal("Fassung 2", cut.Find("p.epos-kihinweis-fassung").TextContent);
        Assert.Equal("Der Assistent sendet Ihre Frage an einen Dienst von Google.",
                     cut.FindAll("p.epos-kihinweis-absatz")[0].TextContent);
    }

    /// <summary>
    /// Jeder Abschnitt bekommt einen EIGENEN Absatz — acht insgesamt (Einleitung
    /// plus sieben). Stünde alles in einem, wäre der Hinweis eine Textwurst.
    /// </summary>
    [Fact]
    public void Jeder_Abschnitt_bekommt_einen_eigenen_Absatz()
    {
        var cut = Zeigen(mitEinwilligung: true);

        Assert.Equal(8, cut.FindAll("p.epos-kihinweis-absatz").Count);
    }

    /// <summary>
    /// Zeilenumbrüche INNERHALB eines Absatzes bleiben erhalten — die Ressourcen
    /// tragen sie als <c>\n</c>, und mehrere Abschnitte listen damit auf.
    /// </summary>
    [Fact]
    public void Umbrueche_innerhalb_eines_Absatzes_bleiben()
    {
        var cut = Render<KiHinweisDialog>(p => p
            .Add(x => x.MitEinwilligung, true)
            .Add(x => x.Einleitung, "Erste Zeile\nZweite Zeile")
            .Add(x => x.Abschnitte, Abschnitte()));

        Assert.Contains("\n", cut.FindAll("p.epos-kihinweis-absatz")[0].TextContent);
    }

    // ==================================================================
    //  Die zwei Betriebsarten
    // ==================================================================

    /// <summary>
    /// <b>Einwilligung:</b> zwei Knöpfe. „Verstanden und einverstanden" ist der
    /// Hauptknopf, „Abbrechen" daneben.
    /// </summary>
    [Fact]
    public void Mit_Einwilligung_stehen_zwei_Knoepfe()
    {
        var cut = Zeigen(mitEinwilligung: true);

        var texte = cut.FindAll("button.epos-knopf").Select(b => b.TextContent.Trim()).ToArray();

        Assert.Equal(new[] { "Verstanden und einverstanden", "Abbrechen" }, texte);
    }

    /// <summary>
    /// <b>Nachlesen:</b> ein Knopf. Es gibt hier nichts einzuwilligen — der
    /// Anwender liest den Hinweis aus dem Chat heraus nach.
    /// </summary>
    [Fact]
    public void Ohne_Einwilligung_steht_nur_Schliessen()
    {
        var cut = Zeigen(mitEinwilligung: false);

        var knoepfe = cut.FindAll("button.epos-knopf");

        Assert.Single(knoepfe);
        Assert.Equal("Schließen", knoepfe[0].TextContent.Trim());
    }

    /// <summary>„Verstanden und einverstanden" meldet <c>true</c>.</summary>
    [Fact]
    public void Einverstanden_meldet_ja()
    {
        bool? antwort = null;

        var cut = Zeigen(true, beantwortet:
            EventCallback.Factory.Create<bool>(this, a => antwort = a));

        cut.FindAll("button.epos-knopf")[0].Click();

        Assert.True(antwort);
    }

    /// <summary>„Abbrechen" meldet <c>false</c> — und damit geht nichts hinaus.</summary>
    [Fact]
    public void Abbrechen_meldet_nein()
    {
        bool? antwort = null;

        var cut = Zeigen(true, beantwortet:
            EventCallback.Factory.Create<bool>(this, a => antwort = a));

        cut.FindAll("button.epos-knopf")[1].Click();

        Assert.False(antwort);
    }

    /// <summary>
    /// Beim Nachlesen meldet „Schließen" ebenfalls <c>false</c> — dort gibt es
    /// nichts einzuwilligen, und eine versehentliche Einwilligung wäre das
    /// Schlimmste, was der Dialog tun könnte.
    /// </summary>
    [Fact]
    public void Schliessen_beim_Nachlesen_willigt_nicht_ein()
    {
        bool? antwort = null;

        var cut = Zeigen(false, beantwortet:
            EventCallback.Factory.Create<bool>(this, a => antwort = a));

        cut.Find("button.epos-knopf").Click();

        Assert.False(antwort);
    }

    // ==================================================================
    //  Die Standzeile
    // ==================================================================

    /// <summary>
    /// Die Standzeile steht in BEIDEN Betriebsarten da — sie sagt, ob und wann
    /// eingewilligt wurde und in welcher Fassung.
    /// </summary>
    [Theory]
    [InlineData(true, "Noch keine Einwilligung.")]
    [InlineData(false, "Eingewilligt am 04.09.2026 10:00 (Fassung 2).")]
    [InlineData(true, "Eingewilligt am 22.08.2026 08:15 (Fassung 1 von 2) – bitte erneut bestätigen.")]
    public void Die_Standzeile_steht_in_beiden_Betriebsarten(bool mitEinwilligung, string stand)
    {
        var cut = Zeigen(mitEinwilligung, stand);

        Assert.Equal(stand, cut.Find("span.epos-kihinweis-stand").TextContent);
    }

    /// <summary>
    /// Der Textbereich ist fokussierbar und rollbar (der Hinweis ist länger als
    /// jedes Fenster) und meldet sich der Sprachausgabe als Dokument.
    /// </summary>
    [Fact]
    public void Der_Textbereich_ist_fokussierbar_und_gemeldet()
    {
        var cut = Zeigen(mitEinwilligung: true);
        var bereich = cut.Find("div.epos-kihinweis-text");

        Assert.Equal("0", bereich.GetAttribute("tabindex"));
        Assert.Equal("document", bereich.GetAttribute("role"));
        Assert.Equal("Hinweis zur Datenübertragung an den KI-Dienst",
                     bereich.GetAttribute("aria-label"));
    }
}
