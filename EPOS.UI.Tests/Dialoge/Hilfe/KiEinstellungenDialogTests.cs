using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using EPOS.UI.Dialoge.Hilfe;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace EPOS.UI.Tests.Dialoge.Hilfe;

/// <summary>
/// <see cref="KiEinstellungenDialog"/> — Zeuge T-3 (iU9-W15b.4).
///
/// <para><b>Vier Dinge kann die Maske</b>, genau wie ihr Vorläufer: den
/// API-Schlüssel eintragen, das feste Tageslimit ansehen, das Modell neu
/// erkennen lassen und „Rückfallweg B erzwingen" schalten. Acht Kartenzeilen
/// hatte die Feldkarte, und acht Bedienelemente sind es hier.</para>
///
/// <para><b>Zwei Zusagen tragen Gewicht.</b> Der Schlüssel ist maskiert und wird
/// weder vorgeschlagen noch rechtschreibgeprüft (Regel S-2) — ein
/// Rechtschreibprüfer schickt Text an den Dienst des Browsers. Und „Modell neu
/// erkennen" hat einen SEITENEFFEKT, der ein Abbrechen überlebt (Entscheid E-5,
/// Befund W15b-B11): Der Test prüft ihn ausdrücklich, denn er ist die einzige
/// Stelle der Welle, an der ein Abbrechen nicht alles zurücknimmt.</para>
///
/// <para>Die Klasse pinnt die Sprache selbst (Regel seit W8).</para>
/// </summary>
public class KiEinstellungenDialogTests : BunitContext
{
    public KiEinstellungenDialogTests()
    {
        var de = new CultureInfo("de-DE");
        CultureInfo.CurrentCulture = de;
        CultureInfo.CurrentUICulture = de;
        CultureInfo.DefaultThreadCurrentCulture = de;
        CultureInfo.DefaultThreadCurrentUICulture = de;
    }

    private IRenderedComponent<KiEinstellungenDialog> Zeigen(
        string schluessel = "AIzaSyGEHEIM",
        bool wegB = false,
        Func<string, Task<string>>? modellNeu = null,
        EventCallback<KiEinstellungenErgebnis?>? geschlossen = null)
    {
        return Render<KiEinstellungenDialog>(p =>
        {
            p.Add(x => x.Schluessel, schluessel)
             .Add(x => x.Tageslimit, 50)
             .Add(x => x.Modellzeile, "Modell: gemini-2.5-flash-lite (kostengünstige Klasse).")
             .Add(x => x.WegB, wegB)
             .Add(x => x.SchluesselText, "API-Schlüssel (Google AI Studio):")
             .Add(x => x.ModellNeuText, "Modell neu erkennen")
             .Add(x => x.TageslimitText, "Tageslimit je Arbeitsplatz:")
             .Add(x => x.TageslimitFormat, "{0} (fest vorgegeben)")
             .Add(x => x.TageslimitTipp, "Die Grenze steht im Programm und lässt sich nicht anheben.")
             .Add(x => x.HinweisDaten, "Es werden ausschließlich Hilfetexte übertragen.")
             .Add(x => x.HinweisKontingent, "Im kostenlosen Kontingent verwendet der Anbieter die Inhalte.")
             .Add(x => x.WegBText, "Rückfallweg B erzwingen (Modell ohne Werkzeuge)")
             .Add(x => x.OkText, "OK")
             .Add(x => x.AbbrechenText, "Abbrechen");

            if (modellNeu is not null) p.Add(x => x.ModellNeuErkennen, modellNeu);
            if (geschlossen is not null) p.Add(x => x.Geschlossen, geschlossen.Value);
        });
    }

    // ==================================================================
    //  Der Feldbestand
    // ==================================================================

    /// <summary>
    /// Acht Bedienelemente wie in der Feldkarte: Schlüsselfeld, „Modell neu
    /// erkennen", Limitbeschriftung, Limitwert, dreiteiliger Hinweis, Weg-B-Schalter,
    /// OK, Abbrechen.
    /// </summary>
    [Fact]
    public void Der_Feldbestand_ist_vollstaendig()
    {
        var cut = Zeigen();

        Assert.NotNull(cut.Find("input[type=password]"));
        Assert.NotNull(cut.Find("button.epos-kieinst-modellneu"));
        Assert.NotNull(cut.Find("p.epos-kieinst-limit"));
        Assert.NotNull(cut.Find("span.epos-kieinst-limitwert"));
        Assert.Equal(3, cut.FindAll("div.epos-kieinst-hinweis p").Count);
        Assert.NotNull(cut.Find("input[type=checkbox]"));
        Assert.Equal(3, cut.FindAll("button.epos-knopf").Count);   // Modell neu, OK, Abbrechen
    }

    // ==================================================================
    //  S-2  Die Maskierung
    // ==================================================================

    /// <summary>
    /// <b>Regel S-2.</b> Der Schlüssel steht in einem Kennwortfeld, wird nicht
    /// vorgeschlagen und nicht rechtschreibgeprüft. Das Gegenstück des Vorläufers
    /// war <c>UseSystemPasswordChar = true</c> (Designer <c>:88</c>).
    /// </summary>
    [Fact]
    public void Der_Schluessel_steht_maskiert_und_wird_nicht_vorgeschlagen()
    {
        var cut = Zeigen();
        var feld = cut.Find("input[type=password]");

        Assert.Equal("off", feld.GetAttribute("autocomplete"));
        Assert.Equal("false", feld.GetAttribute("spellcheck"));
        Assert.Equal("AIzaSyGEHEIM", feld.GetAttribute("value"));
    }

    // ==================================================================
    //  Das Tageslimit - Anzeige, kein Feld
    // ==================================================================

    /// <summary>
    /// <b>Befund W15b-B26.</b> Das Tageslimit steht bewusst im Code und ist auf
    /// keinem Weg zu ändern — „eine Grenze, die der Begrenzte selbst hochsetzen kann,
    /// ist keine". Es erscheint als Text mit Kurzhinweis, nicht als Eingabefeld.
    /// </summary>
    [Fact]
    public void Das_Tageslimit_ist_Anzeige_und_kein_Feld()
    {
        var cut = Zeigen();
        var wert = cut.Find("span.epos-kieinst-limitwert");

        Assert.Equal("50 (fest vorgegeben)", wert.TextContent);
        Assert.Equal("Die Grenze steht im Programm und lässt sich nicht anheben.",
                     wert.GetAttribute("title"));

        // Kein Zahlen- oder Textfeld fuer das Limit - nur das Kennwortfeld.
        Assert.Single(cut.FindAll("input[type=password]"));
        Assert.Empty(cut.FindAll("input[type=number]"));
        Assert.Empty(cut.FindAll("input[type=text]"));
    }

    // ==================================================================
    //  E-5  "Modell neu erkennen" mit Seiteneffekt
    // ==================================================================

    /// <summary>
    /// <b>Der Seiteneffekt (E-5).</b> „Modell neu erkennen" reicht den EINGETIPPTEN
    /// Schlüssel an den Delegaten weiter — sonst könnte die Modellabfrage gar nicht
    /// laufen — und ersetzt die Modellzeile durch das Ergebnis.
    /// </summary>
    [Fact]
    public void Modell_neu_erkennen_uebergibt_den_eingetippten_Schluessel()
    {
        string? bekommen = null;

        var cut = Zeigen(modellNeu: s =>
        {
            bekommen = s;
            return Task.FromResult("Modell neu erkannt: gemini-3.5-flash-lite.");
        });

        cut.Find("input[type=password]").Input("  AIzaSyNEU  ");
        cut.Find("button.epos-kieinst-modellneu").Click();

        // Getrimmt uebergeben - der Vorlaeufer rief .Text.Trim().
        Assert.Equal("AIzaSyNEU", bekommen);
        Assert.Equal("Modell neu erkannt: gemini-3.5-flash-lite.",
                     cut.FindAll("div.epos-kieinst-hinweis p")[0].TextContent);
    }

    /// <summary>
    /// <b>Und er überlebt ein Abbrechen.</b> Der Delegat hat den Schlüssel bereits
    /// gesetzt; die Komponente kann und soll das nicht zurückdrehen. Das ist die
    /// einzige Stelle der Welle, an der ein Abbrechen nicht alles zurücknimmt —
    /// bitgleich mitgezogen.
    /// </summary>
    [Fact]
    public void Der_Seiteneffekt_ueberlebt_ein_Abbrechen()
    {
        int gerufen = 0;
        KiEinstellungenErgebnis? ergebnis = null;
        bool beantwortet = false;

        var cut = Zeigen(
            modellNeu: _ => { gerufen++; return Task.FromResult("Modell: neu"); },
            geschlossen: EventCallback.Factory.Create<KiEinstellungenErgebnis?>(
                this, e => { ergebnis = e; beantwortet = true; }));

        cut.Find("input[type=password]").Input("AIzaSyNEU");
        cut.Find("button.epos-kieinst-modellneu").Click();
        cut.FindAll("button.epos-knopf").Last().Click();   // Abbrechen

        Assert.Equal(1, gerufen);        // der Schluessel ist gesetzt worden ...
        Assert.True(beantwortet);
        Assert.Null(ergebnis);           // ... und trotzdem wurde abgebrochen
    }

    /// <summary>Ohne Delegat tut der Knopf nichts (und wirft nicht).</summary>
    [Fact]
    public void Ohne_Delegat_tut_Modell_neu_erkennen_nichts()
    {
        var cut = Zeigen();

        cut.Find("button.epos-kieinst-modellneu").Click();

        Assert.Equal("Modell: gemini-2.5-flash-lite (kostengünstige Klasse).",
                     cut.FindAll("div.epos-kieinst-hinweis p")[0].TextContent);
    }

    // ==================================================================
    //  OK und Abbrechen
    // ==================================================================

    /// <summary>
    /// OK liefert den getrimmten Schlüssel und den Schalterstand — mehr gibt die
    /// Komponente nie heraus (Regel S-1). Geschrieben wird in der Hülle.
    /// </summary>
    [Fact]
    public void OK_liefert_Schluessel_und_Schalterstand()
    {
        KiEinstellungenErgebnis? ergebnis = null;

        var cut = Zeigen(geschlossen: EventCallback.Factory.Create<KiEinstellungenErgebnis?>(
            this, e => ergebnis = e));

        cut.Find("input[type=password]").Input("  AIzaSyOK  ");
        cut.Find("input[type=checkbox]").Change(true);
        cut.FindAll("button.epos-knopf").First(b => b.TextContent.Trim() == "OK").Click();

        Assert.NotNull(ergebnis);
        Assert.Equal("AIzaSyOK", ergebnis!.ApiSchluessel);
        Assert.True(ergebnis.WegBErzwingen);
    }

    /// <summary>Abbrechen liefert <c>null</c> — der Aufrufer schreibt dann nichts.</summary>
    [Fact]
    public void Abbrechen_liefert_nichts()
    {
        KiEinstellungenErgebnis? ergebnis = null;
        bool beantwortet = false;

        var cut = Zeigen(geschlossen: EventCallback.Factory.Create<KiEinstellungenErgebnis?>(
            this, e => { ergebnis = e; beantwortet = true; }));

        cut.Find("input[type=password]").Input("AIzaSyEGAL");
        cut.FindAll("button.epos-knopf").Last().Click();

        Assert.True(beantwortet);
        Assert.Null(ergebnis);
    }

    /// <summary>Der Weg-B-Schalter kommt vorbelegt herein und geht so wieder heraus.</summary>
    [Fact]
    public void Weg_B_kommt_vorbelegt_herein()
    {
        KiEinstellungenErgebnis? ergebnis = null;

        var cut = Zeigen(wegB: true, geschlossen:
            EventCallback.Factory.Create<KiEinstellungenErgebnis?>(this, e => ergebnis = e));

        Assert.True(cut.Find("input[type=checkbox]").HasAttribute("checked"));

        cut.FindAll("button.epos-knopf").First(b => b.TextContent.Trim() == "OK").Click();

        Assert.True(ergebnis!.WegBErzwingen);
    }
}
