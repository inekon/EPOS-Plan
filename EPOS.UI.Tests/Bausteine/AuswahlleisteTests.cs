using System.Collections.Generic;
using System.Linq;
using Bunit;
using EPOS.UI.Bausteine;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// <b>Die Auswahlleiste</b> — Konzept Administrationsdialoge, Stufe 3, Vorschlag V8
/// (Schema Zone 4): alle Handlungen an Zeilen an einer Stelle, als DATEN hereingereicht.
///
/// <para>Geprüft: das erste Wort (Fokuszeile gegen „n gewählt"), die leise Zeile ohne
/// Zeile, die weiche Sperre außerhalb der Zeilenzahl und mit Sperrgrund (Kurztext, Klick
/// meldet), die harte Sperre, „kein Rückruf, kein Knopf", „Auswahl aufheben" nur bei
/// gesetzten Kästchen, der eingerastete Umschalter und der Knopf des schmalen Fensters.
/// Kultur gepinnt (Hausregel).</para>
/// </summary>
public class AuswahlleisteTests : EposBunitContext
{
    private readonly List<string> _gerufen = new();
    private readonly List<string> _gesperrt = new();

    private EventCallback Rueckruf(string name)
        => EventCallback.Factory.Create(this, () => _gerufen.Add(name));

    private IReadOnlyList<Auswahlhandlung> Handlungen(string sperrgrund = "", bool aus = false,
                                                     bool? gedrueckt = null) => new[]
    {
        new Auswahlhandlung("Vergleichen", Rueckruf("Vergleichen"))
            { Mindestens = 2, ZahlGrund = "Zwei wählen", Gedrueckt = gedrueckt },
        new Auswahlhandlung("Duplizieren...", Rueckruf("Duplizieren"))
            { Hoechstens = 1, ZahlGrund = "Genau eine", Aus = aus },
        new Auswahlhandlung("Löschen", Rueckruf("Löschen")) { Sperrgrund = sperrgrund, Aus = aus }
    };

    private IRenderedComponent<Auswahlleiste> Aufbauen(string fokus, int anzahl,
                                                       IReadOnlyList<Auswahlhandlung>? handlungen = null,
                                                       bool aufheben = true, bool stammblatt = false)
        => Render<Auswahlleiste>(p =>
        {
            p.Add(x => x.Fokus, fokus)
             .Add(x => x.Anzahl, anzahl)
             .Add(x => x.Handlungen, handlungen ?? Handlungen())
             .Add(x => x.Gesperrt, EventCallback.Factory.Create<string>(this, g => _gesperrt.Add(g)));
            if (aufheben) p.Add(x => x.Aufheben, Rueckruf("Aufheben"));
            if (stammblatt) p.Add(x => x.Stammblatt, Rueckruf("Stammblatt"));
        });

    private static AngleSharp.Dom.IElement Knopf(IRenderedComponent<Auswahlleiste> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    /// <summary>Ohne Fokuszeile und ohne Kästchen: eine leise Zeile, kein Knopf.</summary>
    [Fact]
    public void Ohne_Zeile_steht_nur_die_leise_Zeile()
    {
        var cut = Aufbauen("", 0);

        Assert.Empty(cut.FindAll("button"));
        Assert.Equal("Keine Zeile gewählt", cut.Find(".epos-auswahlleiste-leise").TextContent.Trim());
        Assert.Equal("region", cut.Find(".epos-auswahlleiste").GetAttribute("role"));
    }

    /// <summary>Das erste Wort ist die Fokuszeile — oder „3 gewählt", sobald Kästchen gesetzt sind.</summary>
    [Fact]
    public void Das_erste_Wort_nennt_worauf_die_Leiste_wirkt()
    {
        Assert.Equal("Kessel 7", Aufbauen("Kessel 7", 0).Find(".epos-auswahlleiste-was").TextContent.Trim());
        Assert.Equal("3 gewählt", Aufbauen("Kessel 7", 3).Find(".epos-auswahlleiste-was").TextContent.Trim());
    }

    /// <summary>
    /// <b>Außerhalb der Zeilenzahl weich gesperrt</b>: Mit der Fokuszeile allein geht
    /// „Vergleichen" nicht (zwei nötig) — <c>aria-disabled</c>, nicht <c>disabled</c>, der
    /// Grund im Kurztext, und ein Klick meldet ihn statt die Handlung zu rufen.
    /// </summary>
    [Fact]
    public void Ausserhalb_der_Zeilenzahl_ist_der_Knopf_weich_gesperrt_und_meldet_den_Grund()
    {
        var cut = Aufbauen("Kessel 7", 0);

        var vergleichen = Knopf(cut, "Vergleichen");
        Assert.Equal("true", vergleichen.GetAttribute("aria-disabled"));
        Assert.False(vergleichen.HasAttribute("disabled"));
        Assert.Equal("Zwei wählen", vergleichen.GetAttribute("title"));

        vergleichen.Click();
        Assert.Empty(_gerufen);
        Assert.Equal(new[] { "Zwei wählen" }, _gesperrt);

        // Duplizieren gilt der einen Zeile - hier frei.
        Knopf(cut, "Duplizieren...").Click();
        Assert.Equal(new[] { "Duplizieren" }, _gerufen);
    }

    /// <summary>Mit zwei Kästchen kehrt es sich um: Vergleichen frei, Duplizieren gesperrt.</summary>
    [Fact]
    public void Mit_zwei_Kaestchen_kehrt_sich_die_Sperre_um()
    {
        var cut = Aufbauen("Kessel 7", 2);

        Assert.Null(Knopf(cut, "Vergleichen").GetAttribute("aria-disabled"));
        Assert.Equal("true", Knopf(cut, "Duplizieren...").GetAttribute("aria-disabled"));
        Assert.Equal("Genau eine", Knopf(cut, "Duplizieren...").GetAttribute("title"));

        Knopf(cut, "Vergleichen").Click();
        Assert.Equal(new[] { "Vergleichen" }, _gerufen);
    }

    /// <summary>Ein Sperrgrund des Wirts (ein Auslieferungssatz beim Löschen) sperrt weich.</summary>
    [Fact]
    public void Ein_Sperrgrund_sperrt_weich_mit_seinem_Text()
    {
        var cut = Aufbauen("Kessel 7", 0, Handlungen(sperrgrund: "Auslieferungssatz"));

        var loeschen = Knopf(cut, "Löschen");
        Assert.Equal("true", loeschen.GetAttribute("aria-disabled"));
        Assert.Equal("Auslieferungssatz", loeschen.GetAttribute("title"));
        loeschen.Click();
        Assert.Equal(new[] { "Auslieferungssatz" }, _gesperrt);
        Assert.Empty(_gerufen);
    }

    /// <summary>Im Lesemodus des Wirts sind Duplizieren und Löschen HART gesperrt.</summary>
    [Fact]
    public void Aus_sperrt_hart()
    {
        var cut = Aufbauen("Kessel 7", 0, Handlungen(aus: true));

        Assert.True(Knopf(cut, "Löschen").HasAttribute("disabled"));
        Assert.True(Knopf(cut, "Duplizieren...").HasAttribute("disabled"));
    }

    /// <summary>Kein Rückruf, kein Knopf (Hausregel).</summary>
    [Fact]
    public void Kein_Rueckruf_kein_Knopf()
    {
        var cut = Aufbauen("Kessel 7", 0, new[]
        {
            new Auswahlhandlung("Vergleichen", default),
            new Auswahlhandlung("Löschen", Rueckruf("Löschen"))
        });

        Assert.Equal(new[] { "Löschen" },
                     cut.FindAll(".epos-auswahlleiste-knopf").Select(b => b.TextContent.Trim()));
    }

    /// <summary>
    /// <b>„Auswahl aufheben" nur bei gesetzten Kästchen</b>; ohne sie steht der leise
    /// Hinweis, dass es Kästchen gibt.
    /// </summary>
    [Fact]
    public void Auswahl_aufheben_nur_bei_gesetzten_Kaestchen()
    {
        var ohne = Aufbauen("Kessel 7", 0);
        Assert.Empty(ohne.FindAll(".epos-auswahlleiste-aufheben"));
        Assert.Equal("Kästchen: mehrere wählen", ohne.Find(".epos-auswahlleiste-leise").TextContent.Trim());

        var mit = Aufbauen("Kessel 7", 2);
        mit.Find(".epos-auswahlleiste-aufheben").Click();
        Assert.Contains("Aufheben", _gerufen);
    }

    /// <summary>Ein Umschalter trägt <c>aria-pressed</c> und das Bild des eingerasteten Knopfes.</summary>
    [Fact]
    public void Ein_gedrueckter_Umschalter_traegt_aria_pressed()
    {
        var cut = Aufbauen("Kessel 7", 2, Handlungen(gedrueckt: true));

        var vergleichen = Knopf(cut, "Vergleichen");
        Assert.Equal("true", vergleichen.GetAttribute("aria-pressed"));
        Assert.Contains("epos-knopf--gewaehlt", vergleichen.ClassName ?? "");
        Assert.Null(Knopf(cut, "Löschen").GetAttribute("aria-pressed"));
    }

    /// <summary>„Stammblatt ›" nur mit Delegat, und als Knopf des schmalen Fensters gekennzeichnet.</summary>
    [Fact]
    public void Stammblatt_steht_nur_mit_Delegat_und_nur_schmal()
    {
        Assert.Empty(Aufbauen("Kessel 7", 0).FindAll(".epos-nur-schmal"));

        var cut = Aufbauen("Kessel 7", 0, stammblatt: true);
        var knopf = cut.Find(".epos-nur-schmal");
        Assert.Equal("Stammblatt ›", knopf.TextContent.Trim());
        knopf.Click();
        Assert.Equal(new[] { "Stammblatt" }, _gerufen);
    }

    /// <summary>Ohne Gaben zeichnet der Baustein die leise Zeile (Hausregel „auch ohne Gaben").</summary>
    [Fact]
    public void Ohne_Gaben_zeichnet_die_Leiste()
    {
        var cut = Render<Auswahlleiste>();
        Assert.Single(cut.FindAll(".epos-auswahlleiste-leise"));
    }
}
