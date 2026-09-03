using Bunit;
using EPOS.UI.Bausteine;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// Optionsgruppe (iU9-W1.0) - Ersatz der RadioButton-Gruppen des Bestands.
/// Geprueft wird, was die Masken von ihnen verlangen: alle Optionen sichtbar,
/// genau eine gewaehlt, einzelne sperrbar, jede Wahl gemeldet.
/// </summary>
public class OptionsgruppeTests : BunitContext
{
    private static readonly (int Id, string Text)[] Quellen =
    {
        (1, "Aus Vorlage/Variante:"),
        (2, "Aus Projekt/Anlage:")
    };

    [Fact]
    public void Zeigt_alle_Eintraege_mit_ihrer_Beschriftung()
    {
        var cut = Render<Optionsgruppe>(p => p.Add(x => x.Eintraege, Quellen));

        var knoepfe = cut.FindAll("input[type=radio]");
        Assert.Equal(2, knoepfe.Count);

        var texte = cut.FindAll(".epos-feld-text");
        Assert.Equal("Aus Vorlage/Variante:", texte[0].TextContent);
        Assert.Equal("Aus Projekt/Anlage:", texte[1].TextContent);
    }

    [Fact]
    public void Der_Gruppentitel_steht_in_der_Legende()
    {
        var cut = Render<Optionsgruppe>(p => p
            .Add(x => x.Bezeichnung, "Quelle")
            .Add(x => x.Eintraege, Quellen));

        Assert.Equal("Quelle", cut.Find(".epos-optionsgruppe-titel").TextContent);
        Assert.Equal("Quelle", cut.Find("fieldset").GetAttribute("aria-label"));
    }

    [Fact]
    public void Ohne_Bezeichnung_gibt_es_keine_Legende()
    {
        var cut = Render<Optionsgruppe>(p => p.Add(x => x.Eintraege, Quellen));

        Assert.Empty(cut.FindAll(".epos-optionsgruppe-titel"));
    }

    [Fact]
    public void Die_Gruppe_meldet_sich_als_radiogroup()
    {
        var cut = Render<Optionsgruppe>(p => p.Add(x => x.Eintraege, Quellen));

        Assert.Equal("radiogroup", cut.Find("fieldset").GetAttribute("role"));
    }

    [Fact]
    public void Genau_die_gewaehlte_Option_ist_angehakt()
    {
        var cut = Render<Optionsgruppe>(p => p
            .Add(x => x.Eintraege, Quellen)
            .Add(x => x.Auswahl, 2));

        var knoepfe = cut.FindAll("input[type=radio]");
        Assert.False(knoepfe[0].HasAttribute("checked"));
        Assert.True(knoepfe[1].HasAttribute("checked"));
    }

    [Fact]
    public void Eine_Wahl_meldet_ihre_Id()
    {
        int? erhalten = null;
        var cut = Render<Optionsgruppe>(p => p
            .Add(x => x.Eintraege, Quellen)
            .Add(x => x.Auswahl, 1)
            .Add(x => x.AuswahlChanged, (int? id) => erhalten = id));

        cut.FindAll("input[type=radio]")[1].Change(true);

        Assert.Equal(2, erhalten);
    }

    [Fact]
    public void Alle_Eintraege_teilen_sich_einen_Namen()
    {
        var cut = Render<Optionsgruppe>(p => p.Add(x => x.Eintraege, Quellen));

        var knoepfe = cut.FindAll("input[type=radio]");
        Assert.Equal(knoepfe[0].GetAttribute("name"), knoepfe[1].GetAttribute("name"));
        Assert.False(string.IsNullOrEmpty(knoepfe[0].GetAttribute("name")));
    }

    [Fact]
    public void Zwei_Gruppen_tragen_verschiedene_Namen()
    {
        var eine = Render<Optionsgruppe>(p => p.Add(x => x.Eintraege, Quellen));
        var andere = Render<Optionsgruppe>(p => p.Add(x => x.Eintraege, Quellen));

        Assert.NotEqual(eine.Find("input[type=radio]").GetAttribute("name"),
                        andere.Find("input[type=radio]").GetAttribute("name"));
    }

    [Fact]
    public void Aktiv_false_sperrt_die_ganze_Gruppe()
    {
        var cut = Render<Optionsgruppe>(p => p
            .Add(x => x.Eintraege, Quellen)
            .Add(x => x.Aktiv, false));

        foreach (var knopf in cut.FindAll("input[type=radio]"))
        {
            Assert.True(knopf.HasAttribute("disabled"));
        }
    }

    [Fact]
    public void Eine_gesperrte_Option_bleibt_sichtbar_und_ist_nicht_waehlbar()
    {
        // Vorbild: rbQuelleVorlage.Enabled = _vorlagen.Count > 0.
        int? erhalten = null;
        var cut = Render<Optionsgruppe>(p => p
            .Add(x => x.Eintraege, Quellen)
            .Add(x => x.Gesperrt, new[] { 1 })
            .Add(x => x.AuswahlChanged, (int? id) => erhalten = id));

        var knoepfe = cut.FindAll("input[type=radio]");
        Assert.Equal(2, knoepfe.Count);
        Assert.True(knoepfe[0].HasAttribute("disabled"));
        Assert.False(knoepfe[1].HasAttribute("disabled"));

        knoepfe[0].Change(true);
        Assert.Null(erhalten);
    }
}
