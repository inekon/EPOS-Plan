using System.Globalization;
using Bunit;
using EPOS.UI.Dialoge.Hilfe;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace EPOS.UI.Tests.Dialoge.Hilfe;

/// <summary>
/// <see cref="TextAnzeige"/> — Zeuge T-5 (iU9-W15b.2).
///
/// <para>Die Nur-Lese-Anzeige des Assistenten in ihren beiden Ausprägungen:
/// Aktionsprotokoll (ohne Kopfzeile) und Sendevorschau (mit grauer
/// Hinweiszeile). Genau diese zwei Ausprägungen hatte der Vorläufer
/// <c>Form_TextAnzeige</c> — er war selbst schon die Zusammenlegung zweier
/// wortgleicher Wegwerf-Dialoge aus <c>Form_KiChat</c>.</para>
///
/// <para>Die Klasse pinnt die Sprache selbst (Regel seit W8).</para>
/// </summary>
public class TextAnzeigeTests : BunitContext
{
    public TextAnzeigeTests()
    {
        var de = new CultureInfo("de-DE");
        CultureInfo.CurrentCulture = de;
        CultureInfo.CurrentUICulture = de;
        CultureInfo.DefaultThreadCurrentCulture = de;
        CultureInfo.DefaultThreadCurrentUICulture = de;
    }

    /// <summary>
    /// Ohne Kopfzeile — so war das Aktionsprotokoll aufgebaut (der Vorläufer nahm
    /// dafür <c>kopf = null</c>). Es entsteht kein leerer Absatz.
    /// </summary>
    [Fact]
    public void Ohne_Kopf_gibt_es_keine_Kopfzeile()
    {
        var cut = Render<TextAnzeige>(p => p
            .Add(x => x.Inhalt, "2026-09-04 10:00 projekt_lesen OK")
            .Add(x => x.SchliessenText, "Schließen"));

        Assert.Empty(cut.FindAll("p.epos-textanzeige-kopf"));
    }

    /// <summary>
    /// Mit Kopfzeile — so ist die Sendevorschau aufgebaut. Sie sagt, wohin gesendet
    /// würde und mit welchem Modell.
    /// </summary>
    [Fact]
    public void Mit_Kopf_steht_die_Hinweiszeile_ueber_dem_Text()
    {
        const string kopf = "Gesendet wird an gemini-2.5-flash-lite.\nEndpunkt: generativelanguage.googleapis.com";

        var cut = Render<TextAnzeige>(p => p
            .Add(x => x.Kopf, kopf)
            .Add(x => x.Inhalt, "{ \"contents\": [] }")
            .Add(x => x.SchliessenText, "Schließen"));

        Assert.Equal(kopf, cut.Find("p.epos-textanzeige-kopf").TextContent);
    }

    /// <summary>
    /// Der Text steht in einem NUR-LESE-Feld mit FESTER SCHRITTWEITE — der
    /// Vorläufer setzte Consolas 9 pt und <c>ReadOnly</c>. Beides ist keine
    /// Kosmetik: Das Aktionsprotokoll baut seine Spalten aus Leerzeichen, und
    /// „nur lesen" (statt „gesperrt") lässt den Inhalt markieren und kopieren.
    /// </summary>
    [Fact]
    public void Der_Text_steht_nur_lesbar_in_fester_Schrittweite()
    {
        var cut = Render<TextAnzeige>(p => p
            .Add(x => x.Inhalt, "Zeile 1\nZeile 2")
            .Add(x => x.SchliessenText, "Schließen"));

        var feld = cut.Find("textarea");

        Assert.NotNull(feld.GetAttribute("readonly"));
        Assert.Contains("epos-eingabe--festbreite", feld.ClassName);
        Assert.Contains("Zeile 2", feld.TextContent);
    }

    /// <summary>Die Zeilenzahl kommt von außen — Protokoll und Vorschau waren verschieden hoch.</summary>
    [Fact]
    public void Die_Zeilenzahl_kommt_von_aussen()
    {
        var cut = Render<TextAnzeige>(p => p
            .Add(x => x.Inhalt, "Text")
            .Add(x => x.Zeilen, 30)
            .Add(x => x.SchliessenText, "Schließen"));

        Assert.Equal("30", cut.Find("textarea").GetAttribute("rows"));
    }

    /// <summary>
    /// Ein Klick auf „Schließen" meldet sich beim Wirt — die Komponente schließt
    /// nichts selbst (sie weiß nicht, in welcher Überlagerung sie steht).
    /// </summary>
    [Fact]
    public void Schliessen_meldet_sich_beim_Wirt()
    {
        int gemeldet = 0;

        var cut = Render<TextAnzeige>(p => p
            .Add(x => x.Inhalt, "Text")
            .Add(x => x.SchliessenText, "Schließen")
            .Add(x => x.Geschlossen, EventCallback.Factory.Create(this, () => gemeldet++)));

        var knopf = cut.Find("button.epos-knopf");
        Assert.Equal("Schließen", knopf.TextContent.Trim());

        knopf.Click();

        Assert.Equal(1, gemeldet);
    }
}
