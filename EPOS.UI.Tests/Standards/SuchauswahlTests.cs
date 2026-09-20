using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using EPOS.UI.Standards;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Xunit;

namespace EPOS.UI.Tests.Standards;

/// <summary>
/// <b>Die durchsuchbare Auswahlliste</b> (Auftrag KL-4, Anwenderwunsch 19.09.2026:
/// „Das Dropdown soll auch durchsuchbar sein").
///
/// <para>Geprüft wird, was <c>Auswahlfeld</c> nicht kann und was ein
/// <c>&lt;datalist&gt;</c> nicht leistet: der Tippfilter, der Leerfall, die Wahl als
/// ID, die Tastaturführung (↑ ↓ Enter Esc), das Schließen über die Schließfläche
/// statt über <c>focusout</c> — und die Regel, dass ein gesperrtes Feld gar nicht
/// erst aufgeht.</para>
///
/// <para>Die Kultur ist gepinnt (Hausregel seit iU9-W8).</para>
/// </summary>
public class SuchauswahlTests : EposBunitContext
{
    private static readonly IReadOnlyList<(int Id, string Text)> EINTRAEGE = new[]
    {
        (47, "München"), (17, "Berlin"), (46, "Tübingen"), (50, "Lyon")
    };

    private IRenderedComponent<Suchauswahl> Zeige(
        int? auswahl = null,
        Action<int?>? gewaehlt = null,
        bool aktiv = true,
        IReadOnlyList<(int Id, string Text)>? eintraege = null,
        int hoechstens = 50)
    {
        return Render<Suchauswahl>(p => p
            .Add(x => x.Bezeichnung, "Klimaregion:")
            .Add(x => x.Eintraege, eintraege ?? EINTRAEGE)
            .Add(x => x.Auswahl, auswahl)
            .Add(x => x.Aktiv, aktiv)
            .Add(x => x.VorschlaegeHoechstens, hoechstens)
            .Add(x => x.AuswahlChanged,
                 EventCallback.Factory.Create<int?>(this, w => gewaehlt?.Invoke(w))));
    }

    private static AngleSharp.Dom.IElement Feld(IRenderedComponent<Suchauswahl> cut)
        => cut.Find("input[role='combobox']");

    private static string[] Vorschlaege(IRenderedComponent<Suchauswahl> cut)
        => cut.FindAll("li[role='option']").Select(e => e.TextContent.Trim()).ToArray();

    // =====================================================================
    //  Aufbau und Leerzustand
    // =====================================================================

    /// <summary>
    /// Geschlossen steht nur das Feld da — keine Liste, keine Schließfläche. Das
    /// Feld meldet sich als Kombinationsfeld und sagt, dass es zu ist.
    /// </summary>
    [Fact]
    public void Geschlossen_steht_nur_das_Feld()
    {
        var cut = Zeige();

        Assert.Equal("false", Feld(cut).GetAttribute("aria-expanded"));
        Assert.Empty(cut.FindAll("ul[role='listbox']"));
        Assert.Empty(cut.FindAll(".epos-suchauswahl-schliessflaeche"));
        Assert.Contains("Klimaregion:", cut.Markup);
    }

    /// <summary>
    /// <b>Der gewählte Text steht im Feld</b> — was der Anwender sieht, ist seine
    /// Wahl, nicht ein leerer Platzhalter.
    /// </summary>
    [Fact]
    public void Der_gewaehlte_Text_steht_im_Feld()
    {
        var cut = Zeige(auswahl: 46);

        Assert.Equal("Tübingen", Feld(cut).GetAttribute("value"));
    }

    /// <summary>
    /// <b>Ein Klick öffnet die UNGEFILTERTE Liste</b> — wer klickt statt zu tippen,
    /// will die ganze Liste sehen; genau das tut ein <c>&lt;select&gt;</c>.
    /// </summary>
    [Fact]
    public void Ein_Klick_oeffnet_die_ungefilterte_Liste()
    {
        var cut = Zeige(auswahl: 46);

        Feld(cut).Click();

        Assert.Equal("true", Feld(cut).GetAttribute("aria-expanded"));
        Assert.Equal(new[] { "München", "Berlin", "Tübingen", "Lyon" }, Vorschlaege(cut));
        Assert.Single(cut.FindAll(".epos-suchauswahl-schliessflaeche"));
    }

    // =====================================================================
    //  Der Tippfilter
    // =====================================================================

    /// <summary>
    /// <b>„tüb" führt auf Tübingen</b>: gefiltert wird über <c>Contains</c>,
    /// groß/klein egal — nicht nur über den Anfang.
    /// </summary>
    [Fact]
    public void Tippen_filtert_gross_klein_egal_und_mitten_im_Wort()
    {
        var cut = Zeige();

        // Klein geschrieben, mit Umlaut - und der Treffer steht.
        Feld(cut).Input("tüb");
        Assert.Equal(new[] { "Tübingen" }, Vorschlaege(cut));

        // GROSS geschrieben und MITTEN im Wort: "Tübingen" enthaelt "bin".
        Feld(cut).Input("BIN");
        Assert.Equal(new[] { "Tübingen" }, Vorschlaege(cut));

        // Ein Teil, den zwei Eintraege tragen.
        Feld(cut).Input("n");
        Assert.Equal(new[] { "München", "Berlin", "Tübingen", "Lyon" }, Vorschlaege(cut));
    }

    /// <summary>
    /// <b>Kein Treffer ist kein leeres Loch:</b> Die Liste sagt es — und der Eintrag
    /// trägt keine Wahlrolle, denn wählen lässt er sich nicht.
    /// </summary>
    [Fact]
    public void Ohne_Treffer_sagt_die_Liste_es()
    {
        var cut = Zeige();

        Feld(cut).Input("Reykjavík");

        Assert.Empty(cut.FindAll("li[role='option']"));
        Assert.Equal("Kein Treffer.", cut.Find(".epos-suchauswahl-leer").TextContent.Trim());
    }

    /// <summary>
    /// <b>Gezeichnet wird höchstens, was der Wirt erlaubt</b>: Wer bei einem großen
    /// Katalog nichts eintippt, bekämen sonst Tausende <c>&lt;li&gt;</c> in den Baum.
    /// </summary>
    [Fact]
    public void Die_Vorschlagszahl_ist_gedeckelt()
    {
        var viele = Enumerable.Range(1, 500)
                              .Select(i => (i, "Region " + i.ToString("D3")))
                              .ToList();

        var cut = Zeige(eintraege: viele, hoechstens: 10);

        Feld(cut).Click();

        Assert.Equal(10, cut.FindAll("li[role='option']").Count);
    }

    // =====================================================================
    //  Die Wahl
    // =====================================================================

    /// <summary>
    /// <b>Gemeldet wird die ID</b>, nicht der Text (Hausregel „neue Beziehungen über
    /// IDs") — und danach ist die Liste zu, das Feld zeigt den gewählten Text.
    /// </summary>
    [Fact]
    public void Eine_Wahl_meldet_die_Id_und_schliesst()
    {
        int? gemeldet = null;
        var cut = Zeige(gewaehlt: w => gemeldet = w);

        Feld(cut).Input("Lyon");
        cut.Find("li[role='option']").Click();

        Assert.Equal(50, gemeldet);
        Assert.Empty(cut.FindAll("ul[role='listbox']"));
        Assert.False(cut.Instance.Offen);
    }

    /// <summary>
    /// <b>Der gewählte Eintrag trägt <c>aria-selected</c></b> und jeder ein
    /// <c>@@key</c> auf seiner Id — ein Tastendruck tauscht die Einträge aus, und ohne
    /// Schlüssel patcht Blazor sie an Ort und Stelle (Befund W6-B-4).
    /// </summary>
    [Fact]
    public void Der_gewaehlte_Eintrag_ist_als_solcher_ausgezeichnet()
    {
        var cut = Zeige(auswahl: 17);

        Feld(cut).Click();

        var eintraege = cut.FindAll("li[role='option']");
        Assert.Equal("false", eintraege[0].GetAttribute("aria-selected"));
        Assert.Equal("true", eintraege[1].GetAttribute("aria-selected"));

        // Die Kennungen bleiben ueber einen Filterwechsel stabil und eindeutig.
        Feld(cut).Input("n");
        var kennungen = cut.FindAll("li[role='option']")
                           .Select(e => e.GetAttribute("id")).ToList();
        Assert.Equal(kennungen.Count, kennungen.Distinct().Count());
    }

    /// <summary>
    /// <b>Jeder Eintrag trägt die Berührungsklasse</b> — 44 px stehen im Stilblatt an
    /// <c>.epos-suchauswahl-eintrag</c>, und ohne die Klasse gilt sie nicht.
    /// </summary>
    [Fact]
    public void Jeder_Eintrag_traegt_die_Beruehrungsklasse()
    {
        var cut = Zeige();

        Feld(cut).Click();

        Assert.All(cut.FindAll("li[role='option']"),
                   e => Assert.Contains("epos-suchauswahl-eintrag", e.ClassName ?? ""));
    }

    // =====================================================================
    //  Tastatur
    // =====================================================================

    /// <summary>
    /// <b>↓ öffnet und wandert, ↑ geht zurück, Enter übernimmt</b> — die Führung, die
    /// ein <c>&lt;datalist&gt;</c> dem Browser überlässt.
    /// </summary>
    [Fact]
    public void Pfeiltasten_wandern_und_Enter_uebernimmt()
    {
        int? gemeldet = null;
        var cut = Zeige(gewaehlt: w => gemeldet = w);

        Taste(cut, "ArrowDown");                    // oeffnet und geht auf 0
        Assert.True(cut.Instance.Offen);
        Assert.Equal(0, cut.Instance.Hervorgehoben);

        Taste(cut, "ArrowDown");                    // 1 = Berlin
        Assert.Equal(1, cut.Instance.Hervorgehoben);

        Taste(cut, "ArrowUp");                      // zurueck auf 0 = Muenchen
        Assert.Equal(0, cut.Instance.Hervorgehoben);

        Taste(cut, "Enter");
        Assert.Equal(47, gemeldet);
        Assert.False(cut.Instance.Offen);
    }

    /// <summary>
    /// <b>Enter ohne Hervorhebung tut nichts</b> — es darf nicht die erste Zeile
    /// nehmen, nur weil sie oben steht.
    /// </summary>
    [Fact]
    public void Enter_ohne_Hervorhebung_waehlt_nichts()
    {
        int rufe = 0;
        var cut = Zeige(gewaehlt: _ => rufe++);

        Feld(cut).Click();
        Taste(cut, "Enter");

        Assert.Equal(0, rufe);
        Assert.True(cut.Instance.Offen);
    }

    /// <summary>
    /// <b>Esc schließt nur die Liste</b> und ändert die Wahl nicht — der Dialog
    /// darum herum bleibt offen.
    /// </summary>
    [Fact]
    public void Esc_schliesst_die_Liste_ohne_zu_aendern()
    {
        int rufe = 0;
        var cut = Zeige(auswahl: 46, gewaehlt: _ => rufe++);

        Feld(cut).Input("Ber");
        Assert.True(cut.Instance.Offen);

        Taste(cut, "Escape");

        Assert.False(cut.Instance.Offen);
        Assert.Equal(0, rufe);
        Assert.Equal("Tübingen", Feld(cut).GetAttribute("value"));
    }

    // =====================================================================
    //  Schliessflaeche und Sperre
    // =====================================================================

    /// <summary>
    /// <b>Der Klick daneben schließt</b> — über die Schließfläche, nicht über
    /// <c>focusout</c>: Das feuert auch bei einem Fokuswechsel INNERHALB der Liste,
    /// und auf dem iPad setzt eine Berührung überhaupt keinen Fokus (Befund
    /// W16c-B13).
    /// </summary>
    [Fact]
    public void Der_Klick_auf_die_Schliessflaeche_schliesst()
    {
        var cut = Zeige();

        Feld(cut).Click();
        Assert.True(cut.Instance.Offen);

        cut.Find(".epos-suchauswahl-schliessflaeche").Click();

        Assert.False(cut.Instance.Offen);
        Assert.Empty(cut.FindAll("ul[role='listbox']"));
    }

    /// <summary>
    /// <b>Die hohe Stapelebene hängt an der offenen Liste</b> (Welle GM‑1): Nur
    /// <c>epos-suchauswahl--offen</c> vergibt im Stilblatt die Ebene 41. Trüge das
    /// Feld sie dauerhaft, deckte es die aufgeklappte Klappe des Menübandes ab —
    /// das Band steht selbst auf 41, und bei gleicher Ebene gewinnt, was später im
    /// Seitenaufbau steht.
    /// </summary>
    [Fact]
    public void Die_hohe_Stapelebene_steht_nur_bei_offener_Liste()
    {
        var cut = Zeige();

        Assert.Empty(cut.FindAll("label.epos-suchauswahl--offen"));

        Feld(cut).Click();
        Assert.Single(cut.FindAll("label.epos-suchauswahl--offen"));

        cut.Find(".epos-suchauswahl-schliessflaeche").Click();
        Assert.Empty(cut.FindAll("label.epos-suchauswahl--offen"));
    }

    /// <summary>
    /// <b>Ein gesperrtes Feld geht gar nicht erst auf</b> — weder auf Klick noch auf
    /// Tastendruck.
    /// </summary>
    [Fact]
    public void Gesperrt_oeffnet_das_Feld_nicht()
    {
        var cut = Zeige(aktiv: false);

        Assert.True(Feld(cut).HasAttribute("disabled"));

        Feld(cut).Click();
        Taste(cut, "ArrowDown");

        Assert.False(cut.Instance.Offen);
        Assert.Empty(cut.FindAll("ul[role='listbox']"));
    }

    private static void Taste(IRenderedComponent<Suchauswahl> cut, string taste)
        => cut.Find("input[role='combobox']").KeyDown(new KeyboardEventArgs { Key = taste });
}
