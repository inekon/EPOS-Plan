using System.Collections.Generic;
using Bunit;
using EPOS.UI.Bausteine;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// Rueckfrage (iU9-W4.0) — der Ersatz fuer die Ja/Nein-MessageBox. Bis Welle 3
/// reichte jede Komponente diese Fragen an die Windows-Huelle weiter
/// (A-16 aus W1, A-13 aus W3); hier stehen sie im selben Fenster.
/// </summary>
public class RueckfrageTests : BunitContext
{
    [Fact]
    public void Geschlossen_steht_nichts_im_Baum()
    {
        var cut = Render<Rueckfrage>(p => p
            .Add(x => x.Offen, false)
            .Add(x => x.Frage, "Wirklich löschen?"));

        Assert.Empty(cut.FindAll(".epos-rueckfrage"));
    }

    [Fact]
    public void Offen_steht_die_Frage_als_Meldung_da()
    {
        var cut = Render<Rueckfrage>(p => p
            .Add(x => x.Offen, true)
            .Add(x => x.Frage, "Position „Montage\" löschen?"));

        var text = cut.Find(".epos-rueckfrage-text");
        Assert.Equal("Position „Montage\" löschen?", text.TextContent);
        Assert.Equal("alert", text.GetAttribute("role"));
    }

    [Fact]
    public void Ohne_Abbrechen_stehen_genau_zwei_Knoepfe()
    {
        var cut = Render<Rueckfrage>(p => p
            .Add(x => x.Offen, true)
            .Add(x => x.JaText, "Ja")
            .Add(x => x.NeinText, "Nein"));

        var knoepfe = cut.FindAll(".epos-rueckfrage .epos-knopf");
        Assert.Equal(2, knoepfe.Count);
        Assert.Equal("Ja", knoepfe[0].TextContent);
        Assert.Equal("Nein", knoepfe[1].TextContent);
    }

    [Fact]
    public void Mit_Abbrechen_sind_es_drei()
    {
        var cut = Render<Rueckfrage>(p => p
            .Add(x => x.Offen, true)
            .Add(x => x.MitAbbrechen, true)
            .Add(x => x.AbbrechenText, "Abbrechen"));

        var knoepfe = cut.FindAll(".epos-rueckfrage .epos-knopf");
        Assert.Equal(3, knoepfe.Count);
        Assert.Equal("Abbrechen", knoepfe[2].TextContent);
    }

    [Fact]
    public void Ja_meldet_true_Nein_meldet_false()
    {
        var antworten = new List<bool?>();
        var cut = Render<Rueckfrage>(p => p
            .Add(x => x.Offen, true)
            .Add(x => x.Beantwortet, (bool? a) => antworten.Add(a)));

        cut.FindAll(".epos-rueckfrage .epos-knopf")[0].Click();
        cut.FindAll(".epos-rueckfrage .epos-knopf")[1].Click();

        Assert.Equal(new bool?[] { true, false }, antworten);
    }

    [Fact]
    public void Abbrechen_meldet_null()
    {
        bool? antwort = true;
        var cut = Render<Rueckfrage>(p => p
            .Add(x => x.Offen, true)
            .Add(x => x.MitAbbrechen, true)
            .Add(x => x.Beantwortet, (bool? a) => antwort = a));

        cut.FindAll(".epos-rueckfrage .epos-knopf")[2].Click();

        Assert.Null(antwort);
    }

    [Fact]
    public void Esc_heisst_ohne_Abbrechen_Knopf_dasselbe_wie_Nein()
    {
        bool? antwort = true;
        var cut = Render<Rueckfrage>(p => p
            .Add(x => x.Offen, true)
            .Add(x => x.Beantwortet, (bool? a) => antwort = a));

        cut.Find(".epos-ueberlagerung").KeyDown(key: "Escape");

        Assert.False(antwort);
    }

    [Fact]
    public void Esc_heisst_mit_Abbrechen_Knopf_Abbrechen()
    {
        bool? antwort = true;
        var cut = Render<Rueckfrage>(p => p
            .Add(x => x.Offen, true)
            .Add(x => x.MitAbbrechen, true)
            .Add(x => x.Beantwortet, (bool? a) => antwort = a));

        cut.Find(".epos-ueberlagerung").KeyDown(key: "Escape");

        Assert.Null(antwort);
    }

    [Fact]
    public void Der_Titel_steht_ueber_der_Frage_und_es_gibt_kein_Kreuz()
    {
        var cut = Render<Rueckfrage>(p => p
            .Add(x => x.Offen, true)
            .Add(x => x.TitelText, "Kostenverwaltung"));

        Assert.Equal("Kostenverwaltung", cut.Find(".epos-ueberlagerung-titel").TextContent);
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-zu"));
    }
}
